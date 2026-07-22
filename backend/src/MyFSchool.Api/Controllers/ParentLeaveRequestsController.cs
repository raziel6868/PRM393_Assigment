using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1/parents/me")]
public sealed class ParentLeaveRequestsController(ILeaveRequestAdministrationService leaveService) : ControllerBase
{
    [HttpGet("leave-requests")]
    public async Task<IActionResult> List(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] Guid? studentProfileId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await leaveService.ListMineAsync(userId, studentProfileId, page, pageSize, cancellationToken);
        return PageResult(result);
    }

    [HttpGet("leave-requests/{leaveRequestId:guid}")]
    public async Task<IActionResult> Detail(Guid leaveRequestId, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await leaveService.GetForParentAsync(userId, leaveRequestId, cancellationToken);
        return SingleResult(result, "leaveRequestNotFound");
    }

    [HttpPost("leave-requests")]
    public async Task<IActionResult> Submit(SubmitLeaveRequestBody body, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!SchoolSessionExtensions.TryFromWire(body.StartSession, out var startSession))
            return ValidationProblem("startSession", "Buổi bắt đầu không hợp lệ.");
        if (!SchoolSessionExtensions.TryFromWire(body.EndSession, out var endSession))
            return ValidationProblem("endSession", "Buổi kết thúc không hợp lệ.");
        if (!LeaveReasonCategoryExtensions.TryFromWire(body.ReasonCategory, out var category))
            return ValidationProblem("reasonCategory", "Lý do nghỉ không hợp lệ.");

        var result = await leaveService.SubmitAsync(
            new SubmitLeaveRequestCommand(body.StudentProfileId, body.StartDate, body.EndDate,
                startSession, endSession, category, body.Reason, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        return CreatedResult(result);
    }

    [HttpPost("leave-requests/{leaveRequestId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid leaveRequestId, CancelLeaveRequestBody body, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!TryParseRowVersion(body.RowVersion, out var rowVersion))
            return ValidationProblem("rowVersion", "Phiên bản đơn không hợp lệ.");

        var result = await leaveService.CancelAsync(
            new CancelLeaveRequestCommand(leaveRequestId, rowVersion, userId, HttpContext.TraceIdentifier),
            cancellationToken);
        return SingleResult(result, "leaveRequestNotFound");
    }

    private IActionResult PageResult(OperationResult<LeaveRequestPage> result)
    {
        if (!result.IsSuccess) return ProblemResponse(400, result.ErrorCode!, "Không thể tải danh sách", "Vui lòng thử lại.");
        var page = result.Value!;
        return Ok(new LeaveRequestPageResponse(
            page.Items.Select(LeaveResponseMapping.Map).ToList(),
            page.Page, page.PageSize, page.TotalCount, page.TotalPages));
    }

    private IActionResult SingleResult(OperationResult<LeaveRequestResult> result, string notFoundCode)
    {
        if (result.IsSuccess) return Ok(LeaveResponseMapping.Map(result.Value!));
        if (result.ErrorCode == notFoundCode)
            return ProblemResponse(404, notFoundCode, "Không tìm thấy đơn", "Đơn nghỉ không tồn tại hoặc bạn không có quyền xem.");
        return result.ErrorCode switch
        {
            "leaveRequestNotOwned" => ProblemResponse(403, "leaveRequestNotOwned", "Không có quyền truy cập", "Bạn không phải người gửi đơn này."),
            "leaveRequestNotPending" => ProblemResponse(409, "leaveRequestNotPending", "Đơn đã được xử lý", "Chỉ có thể huỷ đơn đang chờ."),
            "concurrencyConflict" => ProblemResponse(409, "concurrencyConflict", "Dữ liệu đã thay đổi", "Đơn đã được cập nhật bởi thao tác khác."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể xử lý đơn", "Vui lòng thử lại.")
        };
    }

    private IActionResult CreatedResult(OperationResult<LeaveRequestResult> result)
    {
        if (result.IsSuccess)
        {
            var item = result.Value!;
            return Created($"/api/v1/parents/me/leave-requests/{item.Id}", LeaveResponseMapping.Map(item));
        }
        return result.ErrorCode switch
        {
            "studentProfileNotFound" => ProblemResponse(404, "studentProfileNotFound", "Không tìm thấy học sinh", "Hồ sơ học sinh không tồn tại."),
            "leaveRequestAlreadyPending" => ProblemResponse(409, "leaveRequestAlreadyPending", "Đã có đơn đang chờ", "Một đơn cho khoảng ngày này đang được xử lý."),
            "invalidDateRange" => ProblemResponse(400, "invalidDateRange", "Ngày không hợp lệ", "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu."),
            "reasonLengthInvalid" => ProblemResponse(400, "reasonLengthInvalid", "Nội dung không hợp lệ", "Nội dung đơn phải từ 20 đến 500 ký tự."),
            _ => ProblemResponse(400, result.ErrorCode!, "Không thể gửi đơn", "Vui lòng thử lại.")
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

internal static class LeaveResponseMapping
{
    public static LeaveRequestResponse Map(LeaveRequestResult item) => new(
        item.Id, item.StudentProfileId, item.RequesterUserId, item.StartDate, item.EndDate,
        item.StartSession, item.EndSession, item.ReasonCategory, item.Reason, item.DecisionNote,
        item.ReviewerUserId, item.ReviewedAtUtc, item.Status, item.CreatedAtUtc, item.DecidedAtUtc,
        item.RowVersion);
}