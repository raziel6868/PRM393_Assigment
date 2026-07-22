using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Teacher)]
[Route("api/v1/teacher")]
public sealed class TeacherLeaveRequestsController(ILeaveRequestAdministrationService leaveService) : ControllerBase
{
    [HttpGet("leave-requests/queue")]
    public async Task<IActionResult> List(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await leaveService.ListTeacherQueueAsync(userId, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return ProblemResponse(400, result.ErrorCode!, "Không thể tải danh sách", "Vui lòng thử lại.");
        var pageResult = result.Value!;
        return Ok(new LeaveRequestPageResponse(
            pageResult.Items.Select(LeaveResponseMapping.Map).ToList(),
            pageResult.Page, pageResult.PageSize, pageResult.TotalCount, pageResult.TotalPages));
    }

    [HttpGet("leave-requests/{leaveRequestId:guid}")]
    public async Task<IActionResult> Detail(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await leaveService.GetForTeacherAsync(userId, leaveRequestId, cancellationToken);
        if (result.IsSuccess) return Ok(LeaveResponseMapping.Map(result.Value!));
        return result.ErrorCode switch
        {
            "leaveRequestNotFound" => ProblemResponse(404, "leaveRequestNotFound", "Không tìm thấy đơn", "Đơn nghỉ không tồn tại."),
            "leaveRequestAccessDenied" => ProblemResponse(403, "leaveRequestAccessDenied", "Không có quyền truy cập", "Bạn không phụ trách lớp của học sinh này."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể tải đơn", "Vui lòng thử lại.")
        };
    }

    [HttpPost("leave-requests/{leaveRequestId:guid}/decide")]
    public async Task<IActionResult> Decide(
        Guid leaveRequestId,
        DecideLeaveRequestBody body,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryParseRowVersion(body.RowVersion, out var rowVersion))
            return ValidationProblem("rowVersion", "Phiên bản đơn không hợp lệ.");

        var result = await leaveService.DecideAsync(
            new DecideLeaveRequestCommand(leaveRequestId, body.Approve, body.DecisionNote,
                rowVersion, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess) return Ok(LeaveResponseMapping.Map(result.Value!));
        return result.ErrorCode switch
        {
            "leaveRequestNotFound" => ProblemResponse(404, "leaveRequestNotFound", "Không tìm thấy đơn", "Đơn nghỉ không tồn tại."),
            "leaveRequestAccessDenied" => ProblemResponse(403, "leaveRequestAccessDenied", "Không có quyền truy cập", "Bạn không phụ trách lớp của học sinh này."),
            "leaveRequestNotPending" => ProblemResponse(409, "leaveRequestNotPending", "Đơn đã được xử lý", "Đơn đã được duyệt hoặc từ chối."),
            "concurrencyConflict" => ProblemResponse(409, "concurrencyConflict", "Dữ liệu đã thay đổi", "Đơn đã được cập nhật bởi thao tác khác."),
            "rejectionReasonRequired" => ProblemResponse(400, "rejectionReasonRequired", "Thiếu lý do từ chối", "Vui lòng nhập lý do khi từ chối đơn."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể xử lý đơn", "Vui lòng thử lại.")
        };
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private static bool TryParseRowVersion(string value, out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }

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

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}