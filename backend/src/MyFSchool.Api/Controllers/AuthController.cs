using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using MyFSchool.Api.Contracts.Auth;
using MyFSchool.Api.Identity;
using MyFSchool.Application.Identity;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IAuthService authService,
    IOptions<WebOriginOptions> webOriginOptions) : ControllerBase
{
    private const string RefreshCookieName = "myfschool.refresh";

    [AllowAnonymous]
    [EnableRateLimiting("sign-in")]
    [HttpPost("sign-in")]
    public async Task<IActionResult> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseClientType(request.ClientType, out var clientType))
        {
            return ApiValidationProblem("clientType", "Loại ứng dụng không hợp lệ.");
        }
        if (ValidateClientTransport(clientType) is { } transportProblem)
        {
            return transportProblem;
        }

        var result = await authService.SignInAsync(new SignInCommand(
            request.EmailOrUserName,
            request.Password,
            clientType,
            HttpContext.TraceIdentifier), cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "temporaryPasswordExpired"
                ? ApiProblem(401, "temporaryPasswordExpired", "Mật khẩu tạm đã hết hạn", "Vui lòng liên hệ nhà trường để được hỗ trợ.")
                : ApiProblem(401, "invalidCredentials", "Không thể đăng nhập", "Email, tên đăng nhập hoặc mật khẩu không đúng.");
        }

        return Ok(ToResponse(result.Value!, clientType));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseClientType(request.ClientType, out var clientType))
        {
            return ApiValidationProblem("clientType", "Loại ứng dụng không hợp lệ.");
        }
        if (ValidateClientTransport(clientType) is { } transportProblem)
        {
            return transportProblem;
        }

        var refreshToken = clientType == AuthClientType.Web
            ? Request.Cookies[RefreshCookieName]
            : request.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            ClearRefreshCookie();
            return ApiProblem(401, "invalidRefreshToken", "Phiên đăng nhập đã hết hạn", "Vui lòng đăng nhập lại.");
        }

        var result = await authService.RefreshAsync(new RefreshSessionCommand(
            refreshToken,
            clientType,
            HttpContext.TraceIdentifier), cancellationToken);
        if (!result.IsSuccess)
        {
            ClearRefreshCookie();
            return ApiProblem(401, "invalidRefreshToken", "Phiên đăng nhập đã hết hạn", "Vui lòng đăng nhập lại.");
        }

        return Ok(ToResponse(result.Value!, clientType));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseClientType(request.ClientType, out var clientType))
        {
            return ApiValidationProblem("clientType", "Loại ứng dụng không hợp lệ.");
        }
        if (ValidateClientTransport(clientType) is { } transportProblem)
        {
            return transportProblem;
        }
        var refreshToken = clientType == AuthClientType.Web
            ? Request.Cookies[RefreshCookieName]
            : request.RefreshToken;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            await authService.LogoutAsync(
                new LogoutCommand(refreshToken, HttpContext.TraceIdentifier),
                cancellationToken);
        }
        ClearRefreshCookie();
        return NoContent();
    }

    [Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
    [HttpGet("session")]
    public async Task<IActionResult> Session(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
        {
            return ApiProblem(401, "unauthorized", "Chưa đăng nhập", "Vui lòng đăng nhập để tiếp tục.");
        }
        var session = await authService.GetSessionAsync(userId, cancellationToken);
        return session is null
            ? ApiProblem(401, "unauthorized", "Chưa đăng nhập", "Vui lòng đăng nhập để tiếp tục.")
            : Ok(new SessionContextResponse(
                session.UserId,
                session.DisplayName,
                SchoolRoles.ToWire(session.Roles),
                session.PasswordChangeRequired));
    }

    private AuthSessionResponse ToResponse(AuthSession session, AuthClientType clientType)
    {
        if (clientType == AuthClientType.Web && session.RefreshToken is not null && session.RefreshTokenExpiresAtUtc is not null)
        {
            Response.Cookies.Append(RefreshCookieName, session.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/api/v1/auth",
                Expires = session.RefreshTokenExpiresAtUtc
            });
        }
        return new AuthSessionResponse(
            session.UserId,
            session.DisplayName,
            SchoolRoles.ToWire(session.Roles),
            session.PasswordChangeRequired,
            session.AccessToken,
            session.AccessTokenExpiresAtUtc,
            clientType == AuthClientType.Mobile ? session.RefreshToken : null,
            session.RefreshTokenExpiresAtUtc);
    }

    private void ClearRefreshCookie() => Response.Cookies.Delete(RefreshCookieName, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth"
    });

    private static bool TryParseClientType(string value, out AuthClientType clientType)
    {
        clientType = value switch
        {
            "web" => AuthClientType.Web,
            "mobile" => AuthClientType.Mobile,
            _ => default
        };
        return value is "web" or "mobile";
    }

    private ObjectResult ApiProblem(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }

    private BadRequestObjectResult ApiValidationProblem(string field, string message)
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

    private ObjectResult? ValidateClientTransport(AuthClientType clientType)
    {
        var origin = Request.Headers.Origin.ToString();
        if (clientType == AuthClientType.Web)
        {
            return webOriginOptions.Value.IsTrusted(origin)
                ? null
                : ApiProblem(403, "untrustedOrigin", "Nguồn yêu cầu không hợp lệ", "Không thể xác minh nguồn của yêu cầu.");
        }

        return string.IsNullOrWhiteSpace(origin)
            ? null
            : ApiProblem(403, "invalidClientTransport", "Kênh đăng nhập không hợp lệ", "Ứng dụng không thể sử dụng kênh đăng nhập này.");
    }
}
