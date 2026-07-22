using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1")]
public sealed class AttendanceHistoryController(
    IAttendanceAdministrationService attendanceService,
    MyFSchoolDbContext dbContext) : ControllerBase
{
    [HttpGet("students/me/attendance-history")]
    public async Task<IActionResult> GetMyHistory(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (User.IsInRole(SchoolRoles.ToWire(SchoolRoles.Student)))
        {
            var studentProfileId = await ResolveStudentProfileIdAsync(userId, cancellationToken);
            if (studentProfileId is null) return ForbiddenResponse();
            return await BuildHistoryResponseAsync(studentProfileId.Value, page, pageSize, cancellationToken);
        }
        if (User.IsInRole(SchoolRoles.ToWire(SchoolRoles.Parent)))
        {
            if (!Guid.TryParse(Request.Query["studentProfileId"], out var studentProfileId))
                return ValidationProblem("studentProfileId", "Vui lòng chọn hồ sơ học sinh.");
            if (!await IsChildOfParentAsync(userId, studentProfileId, cancellationToken))
                return ForbiddenResponse();
            return await BuildHistoryResponseAsync(studentProfileId, page, pageSize, cancellationToken);
        }
        return ForbiddenResponse();
    }

    private async Task<IActionResult> BuildHistoryResponseAsync(
        Guid studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var result = await attendanceService.GetStudentAttendanceHistoryAsync(studentProfileId, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return ProblemResponse(400, result.ErrorCode!, "Không thể tải lịch sử", "Vui lòng thử lại.");
        var pageResult = result.Value!;
        return Ok(new AttendanceHistoryPageResponse(
            pageResult.Items.Select(item => new AttendanceHistoryItemResponse(
                item.AttendanceDate, item.Session, item.ClassDisplayName, item.Status.ToWire(), item.Note)).ToList(),
            pageResult.Page, pageResult.PageSize, pageResult.TotalCount, pageResult.TotalPages));
    }

    private async Task<Guid?> ResolveStudentProfileIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await dbContext.StudentProfiles
            .Where(profile => profile.UserId == userId && profile.IsActive)
            .Select(profile => (Guid?)profile.Id)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<bool> IsChildOfParentAsync(
        Guid parentUserId,
        Guid studentProfileId,
        CancellationToken cancellationToken) =>
        await (
            from parent in dbContext.ParentProfiles
            join link in dbContext.ParentStudentLinks on parent.Id equals link.ParentProfileId
            where parent.UserId == parentUserId && parent.IsActive &&
                  link.IsActive && link.StudentProfileId == studentProfileId
            select 1).AnyAsync(cancellationToken);

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private BadRequestObjectResult ValidationProblem(string field, string message)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]> { [field] = [message] })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Yêu cầu không hợp lệ",
            Detail = "Vui lòng kiểm tra các trường được đánh dấu."
        };
        problem.Extensions["code"] = "validationFailed";
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return BadRequest(problem);
    }

    private ObjectResult ForbiddenResponse()
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Không có quyền truy cập",
            Detail = "Tài khoản không có quyền xem điểm danh này."
        };
        problem.Extensions["code"] = "forbidden";
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(StatusCodes.Status403Forbidden, problem);
    }

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
