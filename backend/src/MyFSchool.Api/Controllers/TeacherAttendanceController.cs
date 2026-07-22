using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Teacher)]
[Route("api/v1/teacher")]
public sealed class TeacherAttendanceController(IAttendanceAdministrationService attendanceService) : ControllerBase
{
    [HttpGet("classes/{classId:guid}/attendance")]
    public async Task<IActionResult> GetClassRoster(
        Guid classId,
        [FromQuery] DateOnly date,
        [FromQuery] string session,
        CancellationToken cancellationToken)
    {
        if (!SchoolSessionExtensions.TryFromWire(session, out var parsedSession))
            return ValidationProblemResponse("session", "Buổi học không hợp lệ.");
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await attendanceService.GetClassRosterAsync(userId, classId, date, parsedSession, cancellationToken);
        if (result.IsSuccess)
        {
            var roster = result.Value!;
            return Ok(new AttendanceRosterResponse(
                roster.ClassId, roster.ClassCode, roster.AttendanceDate, roster.Session,
                roster.Entries.Select(item => new AttendanceRosterEntryResponse(
                    item.StudentProfileId, item.StudentCode, item.StudentDisplayName,
                    item.Status.ToWire(), item.Note, item.RowVersion)).ToList()));
        }
        return result.ErrorCode switch
        {
            "classNotFound" => ProblemResponse(404, "classNotFound", "Không tìm thấy lớp", "Lớp học không tồn tại."),
            "classAccessDenied" => ProblemResponse(403, "classAccessDenied", "Không có quyền truy cập", "Bạn không được phân công lớp này."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể tải danh sách", "Vui lòng thử lại.")
        };
    }

    [HttpPost("classes/{classId:guid}/attendance")]
    public async Task<IActionResult> SaveClassAttendance(
        Guid classId,
        SaveAttendanceRequest request,
        CancellationToken cancellationToken)
    {
        if (!SchoolSessionExtensions.TryFromWire(request.Session, out var parsedSession))
            return ValidationProblemResponse("session", "Buổi học không hợp lệ.");
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var entries = new List<AttendanceEntryUpdate>(request.Entries.Count);
        foreach (var item in request.Entries)
        {
            if (!AttendanceStatusExtensions.TryFromWire(item.Status, out var status))
                return ValidationProblemResponse("entries", "Trạng thái điểm danh không hợp lệ.");
            entries.Add(new AttendanceEntryUpdate(item.StudentProfileId, status, item.Note, item.RowVersion));
        }

        var result = await attendanceService.SaveClassAttendanceAsync(
            new SaveAttendanceCommand(classId, request.AttendanceDate, parsedSession, entries,
                userId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess)
        {
            var saved = result.Value!;
            return Ok(new AttendanceSaveResponse(
                saved.ClassId, saved.AttendanceDate, saved.Session, saved.SavedCount, saved.UnmarkedCount));
        }
        return result.ErrorCode switch
        {
            "classAccessDenied" => ProblemResponse(403, "classAccessDenied", "Không có quyền truy cập", "Bạn không được phân công lớp này."),
            "studentNotEnrolled" => ProblemResponse(400, "studentNotEnrolled", "Học sinh không thuộc lớp", "Vui lòng kiểm tra danh sách học sinh."),
            "concurrencyConflict" => ProblemResponse(409, "concurrencyConflict", "Dữ liệu đã thay đổi", "Bảng điểm danh đã được cập nhật bởi thao tác khác."),
            "unmarkedEntryNotAllowed" => ProblemResponse(400, "unmarkedEntryNotAllowed", "Có học sinh chưa được đánh dấu", "Vui lòng xem lại danh sách trước khi lưu."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể lưu điểm danh", "Vui lòng thử lại.")
        };
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private BadRequestObjectResult ValidationProblemResponse(string field, string message)
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

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
