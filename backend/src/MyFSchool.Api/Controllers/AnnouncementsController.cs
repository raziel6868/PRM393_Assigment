using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Route("api/v1/announcements")]
[Authorize]
public class AnnouncementsController(
    IAnnouncementAdministrationService adminService,
    IAnnouncementQueryService queryService,
    MyFSchoolDbContext db) : ControllerBase
{
    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private async Task<(Guid userId, string role, Guid? profileId)> GetUserContextAsync()
    {
        if (!TryGetUserId(out var userId))
            return (Guid.Empty, "Student", null);

        var role = User.IsInRole("administrator") ? "Administrator"
            : User.IsInRole("teacher") ? "Teacher"
            : User.IsInRole("parent") ? "Parent"
            : "Student";

        Guid? profileId = null;
        if (role == "Teacher")
            profileId = db.TeacherProfiles.Where(t => t.UserId == userId).Select(t => (Guid?)t.Id).FirstOrDefault();
        else if (role == "Student")
            profileId = db.StudentProfiles.Where(s => s.UserId == userId).Select(s => (Guid?)s.Id).FirstOrDefault();
        else if (role == "Parent")
            profileId = db.ParentProfiles.Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefault();

        return (userId, role, profileId);
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (userId, role, profileId) = await GetUserContextAsync();
        var result = await queryService.GetForUserAsync(userId, role, profileId, page, pageSize, ct);

        var items = result.Items.Select(i => new AnnouncementListItemDto(
            i.Id, i.Title, i.Body, i.Audience, i.TargetClassName,
            i.AuthorDisplayName, i.CreatedAtUtc, i.PublishedAtUtc,
            i.ImageUrl, i.ReadCount, i.TotalRecipientCount)).ToList();

        return Ok(new AnnouncementListResponse(items, result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetAnnouncement(Guid id, CancellationToken ct = default)
    {
        var (userId, role, profileId) = await GetUserContextAsync();
        var result = await queryService.GetDetailForUserAsync(id, userId, role, profileId, ct);
        if (!result.IsSuccess || result.Value is null)
            return ProblemResponse(404, result.ErrorCode!, "Khong tim thay", "Vui long thu lai.");

        var announcement = result.Value;
        return Ok(new AnnouncementDetailDto(
            announcement.Id, announcement.Title, announcement.Body, announcement.Audience,
            announcement.TargetClassName, announcement.AuthorDisplayName,
            announcement.CreatedAtUtc, announcement.PublishedAtUtc, announcement.ImageUrl,
            announcement.ReadCount, announcement.TotalRecipientCount,
            Convert.ToBase64String(announcement.RowVersion)));
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await queryService.MarkAsReadAsync(id, userId, ct);
        if (!result.IsSuccess)
            return ProblemResponse(404, result.ErrorCode!, "Khong tim thay", "Vui long thu lai.");
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var count = await queryService.GetUnreadCountAsync(userId, ct);
        return Ok(new UnreadCountResponse(count));
    }

    [HttpPost]
    [Authorize(Roles = "administrator,teacher")]
    public async Task<IActionResult> CreateAnnouncement(
        [FromBody] CreateAnnouncementRequest request,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var validation = request.Validate();
        if (validation.Count > 0)
            return BadRequest(new { errors = validation });

        var command = new CreateAnnouncementCommand(
            request.title, request.body, request.audience, request.targetClassId, request.imageUrl);

        var result = await adminService.CreateAsync(command, userId, ct);
        if (!result.IsSuccess || result.Value is null)
            return ProblemResponse(400, result.ErrorCode!, "Loi", "Vui long thu lai.");

        var announcement = result.Value;
        return Created($"/api/v1/announcements/{announcement.Id}", new AnnouncementDetailDto(
            announcement.Id, announcement.Title, announcement.Body, announcement.Audience,
            announcement.TargetClassName, announcement.AuthorDisplayName,
            announcement.CreatedAtUtc, announcement.PublishedAtUtc, announcement.ImageUrl,
            announcement.ReadCount, announcement.TotalRecipientCount,
            Convert.ToBase64String(announcement.RowVersion)));
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "administrator,teacher")]
    public async Task<IActionResult> PublishAnnouncement(
        Guid id,
        [FromBody] PublishAnnouncementRequest request,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (request.deliveryChannels == null || request.deliveryChannels.Count == 0)
            return ProblemResponse(400, "noChannels", "Loi", "Can chon it nhat mot kenh gui.");

        byte[] rowVersion;
        try
        {
            rowVersion = Convert.FromBase64String(request.rowVersion);
        }
        catch (FormatException)
        {
            return ProblemResponse(400, "invalidRowVersion", "Lỗi dữ liệu", "Phiên bản thông báo không hợp lệ.");
        }
        var command = new PublishAnnouncementCommand(
            id, request.deliveryChannels.Select(c => new DeliveryChannelInfo(c.channel)).ToList(), rowVersion);

        var result = await adminService.PublishAsync(command, userId, ct);
        if (!result.IsSuccess)
            return ProblemResponse(400, result.ErrorCode!, "Loi", "Vui long thu lai.");

        return NoContent();
    }

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
