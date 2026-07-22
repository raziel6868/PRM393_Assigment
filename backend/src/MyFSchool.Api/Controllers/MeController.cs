using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1")]
public sealed class MeController(
    ISchoolScopeQueryService scopeService,
    ILeaveRequestAdministrationService leaveService) : ControllerBase
{
    [HttpGet("me/classes")]
    public async Task<IActionResult> GetClasses(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (User.IsInRole(SchoolRoles.ToWire(SchoolRoles.Student)))
        {
            var studentScopes = await scopeService.GetStudentClassesAsync(userId, cancellationToken);
            return Ok(studentScopes.Select(item => new StudentClassScopeResponse(
                item.ClassId, item.ClassCode, item.ClassDisplayName,
                item.SchoolYearId, item.SchoolYearCode)));
        }
        if (User.IsInRole(SchoolRoles.ToWire(SchoolRoles.Teacher)))
        {
            var teacherScopes = await scopeService.GetTeacherClassesAsync(userId, cancellationToken);
            return Ok(teacherScopes.Select(item => new TeacherClassScopeResponse(
                item.ClassId, item.ClassCode, item.ClassDisplayName,
                item.SubjectId, item.SubjectCode, item.SubjectDisplayName,
                item.SchoolYearId, item.SchoolYearCode, item.IsHomeroom)));
        }
        if (User.IsInRole(SchoolRoles.ToWire(SchoolRoles.Parent)))
        {
            var parentScopes = await scopeService.GetParentChildrenClassesAsync(userId, cancellationToken);
            return Ok(parentScopes.Select(item => new ParentChildClassScopeResponse(
                item.StudentProfileId, item.StudentUserId, item.StudentDisplayName,
                item.StudentCode, item.ClassId, item.ClassCode, item.ClassDisplayName,
                item.SchoolYearId, item.SchoolYearCode)));
        }

        return Ok(Array.Empty<object>());
    }

    [HttpGet("students/me/leave-requests")]
    public async Task<IActionResult> ListOwnLeaveRequests(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var result = await leaveService.ListMineAsync(userId, null, page, pageSize, cancellationToken);
        if (!result.IsSuccess) return ProblemResponse(400, result.ErrorCode!, "Không thể tải đơn", "Vui lòng thử lại.");
        var pageResult = result.Value!;
        return Ok(new LeaveRequestPageResponse(
            pageResult.Items.Select(LeaveResponseMapping.Map).ToList(),
            pageResult.Page, pageResult.PageSize, pageResult.TotalCount, pageResult.TotalPages));
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
