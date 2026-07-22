using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1/clubs")]
public sealed class ClubsController(IClubAdministrationService clubService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await clubService.ListPublicAsync(search, category, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải danh sách CLB", "Vui lòng thử lại.");
        var p = result.Value!;
        return Ok(new ClubPageResponse(
            p.Items.Select(Map).ToList(), p.Page, p.PageSize, p.TotalCount, p.TotalPages));
    }

    [HttpGet("{clubId:guid}")]
    public async Task<IActionResult> Detail(Guid clubId, CancellationToken cancellationToken)
    {
        Guid? userId = TryGetUserId(out var uid) ? uid : null;
        var result = await clubService.GetDetailAsync(clubId, userId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "clubNotFound")
                return ProblemResponse(404, "clubNotFound", "Không tìm thấy CLB", "CLB không tồn tại hoặc không còn hoạt động.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tải chi tiết CLB", "Vui lòng thử lại.");
        }
        return Ok(MapDetail(result.Value!));
    }

    [HttpPost("{clubId:guid}/join")]
    public async Task<IActionResult> Join(Guid clubId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await clubService.JoinAsync(
            new JoinClubCommand(clubId, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "clubNotFound")
                return ProblemResponse(404, "clubNotFound", "Không tìm thấy CLB", "CLB không tồn tại.");
            if (result.ErrorCode == "studentProfileNotFound")
                return ProblemResponse(403, "studentProfileNotFound", "Không phải học sinh", "Chỉ học sinh mới có thể tham gia CLB.");
            if (result.ErrorCode == "alreadyMember")
                return ProblemResponse(409, "alreadyMember", "Đã là thành viên", "Bạn đã tham gia CLB này.");
            if (result.ErrorCode == "requestPending")
                return ProblemResponse(409, "requestPending", "Đang chờ duyệt", "Yêu cầu tham gia đang được xử lý.");
            if (result.ErrorCode == "clubAtCapacity")
                return ProblemResponse(409, "clubAtCapacity", "CLB đã đầy", "Số lượng thành viên đã đạt giới hạn.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể tham gia CLB", "Vui lòng thử lại.");
        }
        return Ok(MapDetail(result.Value!));
    }

    [HttpPost("{clubId:guid}/leave")]
    public async Task<IActionResult> Leave(Guid clubId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await clubService.LeaveAsync(
            new LeaveClubCommand(clubId, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "clubNotFound")
                return ProblemResponse(404, "clubNotFound", "Không tìm thấy CLB", "CLB không tồn tại.");
            if (result.ErrorCode == "studentProfileNotFound")
                return ProblemResponse(403, "studentProfileNotFound", "Không phải học sinh", "Chỉ học sinh mới có thể rời CLB.");
            if (result.ErrorCode == "notAMember")
                return ProblemResponse(409, "notAMember", "Không phải thành viên", "Bạn chưa tham gia CLB này.");
            return ProblemResponse(400, result.ErrorCode!, "Không thể rời CLB", "Vui lòng thử lại.");
        }
        return Ok(MapDetail(result.Value!));
    }

    private static ClubResponse Map(ClubResult c) => new(
        c.Id, c.Code, c.DisplayName, c.Description, c.Category,
        c.MaxMembers, c.CurrentMemberCount, c.IsActive, c.RowVersion);

    private static ClubDetailResponse MapDetail(ClubDetailResult c) => new(
        c.Id, c.Code, c.DisplayName, c.Description, c.Category,
        c.MaxMembers, c.CurrentMemberCount, c.IsActive, c.RowVersion,
        c.MembershipStatus, c.JoinedAtUtc);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
