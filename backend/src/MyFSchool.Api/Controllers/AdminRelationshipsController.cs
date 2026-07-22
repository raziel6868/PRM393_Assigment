using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.Relationships;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Administrator)]
[Route("api/v1/admin")]
public sealed class AdminRelationshipsController(
    IIdentityRelationshipAdministrationService relationshipService) : ControllerBase
{
    [HttpPost("identity-profiles/teachers")]
    public async Task<IActionResult> CreateTeacherProfile(
        CreateTeacherProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (request.UserId == Guid.Empty) return ValidationProblemResponse("userId", "Tài khoản không hợp lệ.");
        var result = await relationshipService.CreateTeacherProfileAsync(
            new CreateIdentityProfileCommand(request.UserId, request.EmployeeCode, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        return ProfileResult(result, "teachers", profile => new TeacherProfileResponse(
            profile.Id, profile.UserId, profile.Code, profile.IsActive));
    }

    [HttpPost("identity-profiles/students")]
    public async Task<IActionResult> CreateStudentProfile(
        CreateStudentProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (request.UserId == Guid.Empty) return ValidationProblemResponse("userId", "Tài khoản không hợp lệ.");
        var result = await relationshipService.CreateStudentProfileAsync(
            new CreateIdentityProfileCommand(request.UserId, request.StudentCode, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        return ProfileResult(result, "students", profile => new StudentProfileResponse(
            profile.Id, profile.UserId, profile.Code, profile.IsActive));
    }

    [HttpPost("identity-profiles/parents")]
    public async Task<IActionResult> CreateParentProfile(
        CreateParentProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (request.UserId == Guid.Empty) return ValidationProblemResponse("userId", "Tài khoản không hợp lệ.");
        var result = await relationshipService.CreateParentProfileAsync(
            new CreateIdentityProfileCommand(request.UserId, request.ParentCode, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        return ProfileResult(result, "parents", profile => new ParentProfileResponse(
            profile.Id, profile.UserId, profile.Code, profile.IsActive));
    }

    [HttpPost("parent-student-links")]
    public async Task<IActionResult> CreateLink(
        CreateParentStudentLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (request.ParentProfileId == Guid.Empty)
            return ValidationProblemResponse("parentProfileId", "Hồ sơ phụ huynh không hợp lệ.");
        if (request.StudentProfileId == Guid.Empty)
            return ValidationProblemResponse("studentProfileId", "Hồ sơ học sinh không hợp lệ.");
        if (!TryParseRelationship(request.Relationship, out var relationship))
            return ValidationProblemResponse("relationship", "Mối quan hệ không hợp lệ.");

        var result = await relationshipService.CreateParentStudentLinkAsync(
            new CreateParentStudentLinkCommand(
                request.ParentProfileId,
                request.StudentProfileId,
                relationship,
                request.IsPrimaryContact,
                actorUserId,
                HttpContext.TraceIdentifier),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return result.ErrorCode == "profileNotFound"
                ? ProblemResponse(404, "notFound", "Không tìm thấy hồ sơ", "Hồ sơ không tồn tại hoặc không còn hoạt động.")
                : ProblemResponse(409, "relationshipAlreadyExists", "Liên kết đã tồn tại", "Phụ huynh và học sinh đã có liên kết.");
        }
        var link = result.Value!;
        return Created($"/api/v1/admin/parent-student-links/{link.Id}", ToResponse(link));
    }

    [HttpPatch("parent-student-links/{linkId:guid}")]
    public async Task<IActionResult> UpdateLink(
        Guid linkId,
        UpdateParentStudentLinkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        if (!TryParseRelationship(request.Relationship, out var relationship))
            return ValidationProblemResponse("relationship", "Mối quan hệ không hợp lệ.");
        if (!TryParseRowVersion(request.RowVersion, out var rowVersion))
            return ValidationProblemResponse("rowVersion", "Phiên bản liên kết không hợp lệ.");

        var result = await relationshipService.UpdateParentStudentLinkAsync(
            new UpdateParentStudentLinkCommand(
                linkId,
                relationship,
                request.IsPrimaryContact,
                request.IsActive,
                rowVersion,
                actorUserId,
                HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess) return Ok(ToResponse(result.Value!));
        return result.ErrorCode == "relationshipNotFound"
            ? ProblemResponse(404, "notFound", "Không tìm thấy liên kết", "Liên kết không tồn tại.")
            : ProblemResponse(409, "concurrencyConflict", "Dữ liệu đã thay đổi", "Liên kết đã được cập nhật bởi một thao tác khác.");
    }

    private IActionResult ProfileResult<TResponse>(
        OperationResult<IdentityProfileResult> result,
        string routeSegment,
        Func<IdentityProfileResult, TResponse> map)
    {
        if (result.IsSuccess)
        {
            var profile = result.Value!;
            return Created($"/api/v1/admin/identity-profiles/{routeSegment}/{profile.Id}", map(profile));
        }
        return result.ErrorCode switch
        {
            "accountNotFound" => ProblemResponse(404, "notFound", "Không tìm thấy tài khoản", "Tài khoản không tồn tại hoặc không còn hoạt động."),
            "profileRoleMismatch" => ProblemResponse(400, "profileRoleMismatch", "Vai trò không phù hợp", "Tài khoản không có vai trò phù hợp với hồ sơ."),
            _ => ProblemResponse(409, "profileAlreadyExists", "Hồ sơ đã tồn tại", "Tài khoản hoặc mã hồ sơ đã được sử dụng.")
        };
    }

    private bool TryGetActorUserId(out Guid actorUserId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out actorUserId);

    private static bool TryParseRelationship(string value, out GuardianRelationship relationship)
    {
        relationship = value switch
        {
            "father" => GuardianRelationship.Father,
            "mother" => GuardianRelationship.Mother,
            "guardian" => GuardianRelationship.Guardian,
            "other" => GuardianRelationship.Other,
            _ => default
        };
        return value is "father" or "mother" or "guardian" or "other";
    }

    private static string ToWire(GuardianRelationship relationship) => relationship switch
    {
        GuardianRelationship.Father => "father",
        GuardianRelationship.Mother => "mother",
        GuardianRelationship.Guardian => "guardian",
        GuardianRelationship.Other => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(relationship))
    };

    private static ParentStudentLinkResponse ToResponse(ParentStudentLinkResult link) => new(
        link.Id,
        link.ParentProfileId,
        link.StudentProfileId,
        ToWire(link.Relationship),
        link.IsPrimaryContact,
        link.IsActive,
        link.CreatedAtUtc,
        link.RowVersion);

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
