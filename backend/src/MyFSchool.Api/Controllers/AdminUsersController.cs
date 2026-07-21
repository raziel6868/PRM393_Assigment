using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.Auth;
using MyFSchool.Application.Identity;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Administrator)]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController(IAccountAdministrationService accountAdministrationService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Provision(ProvisionUserRequest request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var actorUserId))
        {
            return Unauthorized();
        }
        var roles = new List<string>(request.Roles.Count);
        foreach (var role in request.Roles)
        {
            if (!SchoolRoles.TryFromWire(role, out var internalRole))
            {
                return ValidationProblemResponse("roles", "Vai trò không hợp lệ.");
            }
            roles.Add(internalRole);
        }
        if (roles.Count == 0)
        {
            return ValidationProblemResponse("roles", "Vui lòng chọn ít nhất một vai trò.");
        }
        var result = await accountAdministrationService.ProvisionAsync(new ProvisionUserCommand(
            request.DisplayName,
            request.UserName,
            request.Email,
            roles,
            actorUserId,
            HttpContext.TraceIdentifier), cancellationToken);
        if (!result.IsSuccess)
        {
            var status = result.ErrorCode == "accountAlreadyExists" ? 409 : 400;
            var problem = new ProblemDetails
            {
                Status = status,
                Title = status == 409 ? "Tài khoản đã tồn tại" : "Không thể tạo tài khoản",
                Detail = status == 409
                    ? "Tên đăng nhập hoặc email đã được sử dụng."
                    : "Vui lòng kiểm tra thông tin tài khoản và vai trò."
            };
            problem.Extensions["code"] = result.ErrorCode;
            problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
            return StatusCode(status, problem);
        }

        var user = result.Value!;
        return Created($"/api/v1/admin/users/{user.UserId}", new ProvisionedUserResponse(
            user.UserId,
            user.DisplayName,
            user.UserName,
            user.Email,
            SchoolRoles.ToWire(user.Roles),
            user.TemporaryPassword,
            user.TemporaryPasswordExpiresAtUtc));
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
}
