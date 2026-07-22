using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MyFSchool.Api.Identity;
using MyFSchool.Application.Identity;
using MyFSchool.Infrastructure;
using MyFSchool.Infrastructure.Configuration;
using MyFSchool.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => JsonNamingPolicy.CamelCase.ConvertName(entry.Key),
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "Giá trị không hợp lệ."
                        : error.ErrorMessage)
                    .ToArray());
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Yêu cầu không hợp lệ",
            Detail = "Vui lòng kiểm tra các trường được đánh dấu."
        };
        problem.Extensions["code"] = "validationFailed";
        problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem);
    };
});
var configuredWebOrigins = builder.Configuration
    .GetSection(WebOriginOptions.SectionName)
    .Get<WebOriginOptions>() ?? new WebOriginOptions();
builder.Services
    .AddOptions<WebOriginOptions>()
    .Bind(builder.Configuration.GetSection(WebOriginOptions.SectionName))
    .Validate(options => WebOriginOptions.AreValid(options.AllowedOrigins),
        "WebOrigins__AllowedOrigins must contain at least one valid HTTP or HTTPS origin")
    .ValidateOnStart();
builder.Services.AddCors(options => options.AddPolicy("web-client", policy => policy
    .WithOrigins(configuredWebOrigins.AllowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));
var configuredSigningKey = builder.Configuration["Auth:JwtSigningKey"] ?? string.Empty;
var signingKey = Encoding.UTF8.GetByteCount(configuredSigningKey) >= 32
    ? configuredSigningKey
    : new string('0', 32);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var subject = context.Principal?.FindFirst("sub")?.Value;
                var sessionVersion = context.Principal?.FindFirst(SessionVersion.ClaimName)?.Value;
                var restrictedClaim = context.Principal?.FindFirst("passwordChangeRequired")?.Value;
                if (!Guid.TryParse(subject, out var userId))
                {
                    context.Fail("Invalid session.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<AppUser>>();
                var user = await userManager.FindByIdAsync(userId.ToString());
                var expectedRestrictedClaim = user?.MustChangePassword == true ? "true" : "false";
                if (user is null || !user.IsActive ||
                    !SessionVersion.Matches(sessionVersion, user.SecurityStamp ?? string.Empty) ||
                    !string.Equals(restrictedClaim, expectedRestrictedClaim, StringComparison.Ordinal))
                {
                    context.Fail("Invalid session.");
                }
            },
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var problemService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = CreateAuthProblem(401, "unauthorized", "Chưa đăng nhập", "Vui lòng đăng nhập để tiếp tục.")
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                var problemService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
                await problemService.WriteAsync(new ProblemDetailsContext
                {
                    HttpContext = context.HttpContext,
                    ProblemDetails = CreateAuthProblem(403, "forbidden", "Không có quyền truy cập", "Bạn không có quyền thực hiện thao tác này.")
                });
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("passwordChangeRequired", "false")
        .Build();
    options.AddPolicy(SchoolPolicies.AuthenticatedSession, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(SchoolPolicies.Administrator, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(SchoolRoles.ToWire(SchoolRoles.Administrator))
        .RequireClaim("passwordChangeRequired", "false"));
    options.AddPolicy(SchoolPolicies.Parent, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(SchoolRoles.ToWire(SchoolRoles.Parent))
        .RequireClaim("passwordChangeRequired", "false"));
    options.AddPolicy(SchoolPolicies.Teacher, policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(SchoolRoles.ToWire(SchoolRoles.Teacher))
        .RequireClaim("passwordChangeRequired", "false"));
});
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("sign-in", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("password-help", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        var problemService = context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context.HttpContext,
            ProblemDetails = CreateAuthProblem(
                429,
                "tooManyRequests",
                "Quá nhiều yêu cầu",
                "Vui lòng chờ một lát rồi thử lại.")
        });
    };
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        if (context.ProblemDetails.Status == StatusCodes.Status404NotFound)
        {
            context.ProblemDetails.Extensions["code"] = "notFound";
            context.ProblemDetails.Title = "Không tìm thấy tài nguyên";
            context.ProblemDetails.Detail = "Tài nguyên bạn yêu cầu không tồn tại hoặc bạn không có quyền truy cập.";
        }
        else if (context.ProblemDetails.Status >= StatusCodes.Status500InternalServerError)
        {
            context.ProblemDetails.Extensions["code"] = "unexpectedError";
            context.ProblemDetails.Title = "Không thể xử lý yêu cầu";
            context.ProblemDetails.Detail = "Hệ thống gặp sự cố. Vui lòng thử lại sau.";
        }
        else
        {
            context.ProblemDetails.Extensions.TryAdd("code", "requestError");
            context.ProblemDetails.Title = "Yêu cầu không hợp lệ";
            context.ProblemDetails.Detail ??= "Vui lòng kiểm tra thông tin và thử lại.";
        }
    };
});
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        return Task.CompletedTask;
    });

    await next(context);
});
app.UseCors("web-client");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<IIdentityBootstrapper>()
        .InitializeAsync(CancellationToken.None);
}

app.Run();

static ProblemDetails CreateAuthProblem(int status, string code, string title, string detail)
{
    var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
    problem.Extensions["code"] = code;
    return problem;
}
