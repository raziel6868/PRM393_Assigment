using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1/schedule")]
public sealed class TimetableController(ITimetableQueryService timetableService) : ControllerBase
{
    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyTimetable(
        [FromQuery] string weekStart,
        [FromQuery] Guid? studentProfileId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!DateOnly.TryParseExact(weekStart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            return BadRequest(new ProblemDetails { Status = 400, Title = "Ngày không hợp lệ", Detail = "Định dạng ngày phải là yyyy-MM-dd." });

        var role = ResolveRole();
        var result = await timetableService.GetWeekTimetableAsync(userId, role, studentProfileId, parsedDate, cancellationToken);
        return Ok(result);
    }

    private string ResolveRole()
    {
        if (User.IsInRole("student")) return "student";
        if (User.IsInRole("teacher")) return "teacher";
        if (User.IsInRole("parent")) return "parent";
        return "unknown";
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);
}

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1/events")]
public sealed class EventsController(IEventQueryService eventService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = TryGetUserId(out var uid) ? uid : (Guid?)null;
        var role = ResolveRole();

        var result = await eventService.GetUpcomingEventsAsync(userId, role, null, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
            return StatusCode(400, new ProblemDetails { Status = 400, Title = "Không thể tải sự kiện" });
        return Ok(result.Value!);
    }

    [HttpGet("{eventId:guid}")]
    public async Task<IActionResult> GetDetail(Guid eventId, CancellationToken cancellationToken)
    {
        var result = await eventService.GetEventDetailAsync(eventId, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorCode == "eventNotFound")
                return StatusCode(404, new ProblemDetails { Status = 404, Title = "Không tìm thấy sự kiện", Detail = "Sự kiện không tồn tại." });
            return StatusCode(400, new ProblemDetails { Status = 400, Title = "Không thể tải chi tiết sự kiện" });
        }
        return Ok(result.Value!);
    }

    private string ResolveRole()
    {
        if (User.IsInRole("student")) return "student";
        if (User.IsInRole("teacher")) return "teacher";
        if (User.IsInRole("parent")) return "parent";
        return "unknown";
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);
}
