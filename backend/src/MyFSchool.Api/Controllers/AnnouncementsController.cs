using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Infrastructure.Identity;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Route("api/v1/announcements")]
[Authorize]
public class AnnouncementsController(
    IAnnouncementAdministrationService adminService,
    IAnnouncementQueryService queryService,
    UserManager<AppUser> userManager,
    MyFSchoolDbContext db) : ControllerBase
{
    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private async Task<(Guid userId, string role, Guid? profileId)> GetUserContextAsync()
    {
        if (!TryGetUserId(out var userId))
            return (Guid.Empty, "Student", null);

        var user = await userManager.FindByIdAsync(userId.ToString());
        var roles = await userManager.GetRolesAsync(user!);
        var role = roles.FirstOrDefault() ?? "Student";

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
        if (!result.IsSuccess)
            return ProblemResponse(404, result.ErrorCode!, "Khong tim thay", "Vui long thu lai.");

        return Ok(new AnnouncementDetailDto(
            result.Value.Id, result.Value.Title, result.Value.Body, result.Value.Audience,
            result.Value.TargetClassName, result.Value.AuthorDisplayName,
            result.Value.CreatedAtUtc, result.Value.PublishedAtUtc, result.Value.ImageUrl,
            result.Value.ReadCount, result.Value.TotalRecipientCount,
            Convert.ToBase64String(result.Value.RowVersion)));
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
    [Authorize(Roles = "Administrator,Teacher")]
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
        if (!result.IsSuccess)
            return ProblemResponse(400, result.ErrorCode!, "Loi", "Vui long thu lai.");

        return Created($"/api/v1/announcements/{result.Value.Id}", new AnnouncementDetailDto(
            result.Value.Id, result.Value.Title, result.Value.Body, result.Value.Audience,
            result.Value.TargetClassName, result.Value.AuthorDisplayName,
            result.Value.CreatedAtUtc, result.Value.PublishedAtUtc, result.Value.ImageUrl,
            result.Value.ReadCount, result.Value.TotalRecipientCount,
            Convert.ToBase64String(result.Value.RowVersion)));
    }

    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Administrator,Teacher")]
    public async Task<IActionResult> PublishAnnouncement(
        Guid id,
        [FromBody] PublishAnnouncementRequest request,
        CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (request.deliveryChannels == null || request.deliveryChannels.Count == 0)
            return ProblemResponse(400, "noChannels", "Loi", "Can chon it nhat mot kenh gui.");

        var rowVersion = Convert.FromBase64String(request.rowVersion);
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
