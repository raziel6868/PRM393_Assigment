using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.Auth;
using MyFSchool.Application.Identity;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Administrator)]
[Route("api/v1/admin/password-help-requests")]
public sealed class AdminPasswordHelpController(
    IAccountAdministrationService accountAdministrationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRequests(
        [FromQuery] string status = "pending",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseStatus(status, out var statusFilter))
        {
            return ValidationProblemResponse("status", "Trạng thái yêu cầu hỗ trợ không hợp lệ.");
        }
        if (page < 1)
        {
            return ValidationProblemResponse("page", "Trang phải lớn hơn hoặc bằng 1.");
        }
        if (pageSize is < 1 or > 100)
        {
            return ValidationProblemResponse("pageSize", "Số bản ghi mỗi trang phải từ 1 đến 100.");
        }

        var result = await accountAdministrationService.GetPasswordHelpRequestsAsync(
            new PasswordHelpQuery(statusFilter, page, pageSize),
            cancellationToken);
        return Ok(new PasswordHelpPageResponse(
            result.Items.Select(item => new PasswordHelpItemResponse(
                item.RequestId,
                item.UserId,
                item.DisplayName,
                item.UserName,
                item.Email,
                ToWireStatus(item.Status),
                item.RequestedAtUtc,
                item.ResolvedAtUtc,
                item.RowVersion)).ToArray(),
            result.Page,
            result.PageSize,
            result.TotalCount,
            result.TotalPages));
    }

    [HttpPost("{requestId:guid}/reject")]
    public async Task<IActionResult> Reject(
        Guid requestId,
        RejectPasswordHelpRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.Confirmed)
        {
            return ValidationProblemResponse("confirmed", "Vui lòng xác nhận từ chối yêu cầu hỗ trợ.");
        }
        if (!TryParseRowVersion(request.RowVersion, out var rowVersion))
        {
            return ValidationProblemResponse("rowVersion", "Phiên bản yêu cầu hỗ trợ không hợp lệ.");
        }
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var actorUserId))
        {
            return Unauthorized();
        }

        var result = await accountAdministrationService.RejectPasswordHelpRequestAsync(
            new RejectPasswordHelpCommand(
                requestId,
                request.Confirmed,
                rowVersion,
                actorUserId,
                HttpContext.TraceIdentifier),
            cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ProblemResponse(
                409,
                "passwordHelpRequestNotPending",
                "Yêu cầu đã thay đổi",
                "Yêu cầu hỗ trợ không còn ở trạng thái chờ xử lý.");
    }

    private static bool TryParseStatus(string value, out PasswordHelpStatusFilter status)
    {
        status = value switch
        {
            "pending" => PasswordHelpStatusFilter.Pending,
            "resolved" => PasswordHelpStatusFilter.Resolved,
            "rejected" => PasswordHelpStatusFilter.Rejected,
            _ => default
        };
        return value is "pending" or "resolved" or "rejected";
    }

    private static string ToWireStatus(PasswordHelpStatusFilter status) => status switch
    {
        PasswordHelpStatusFilter.Pending => "pending",
        PasswordHelpStatusFilter.Resolved => "resolved",
        PasswordHelpStatusFilter.Rejected => "rejected",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

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

    private BadRequestObjectResult ValidationProblemResponse(string field, string message)
    {
        var problem = new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
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
