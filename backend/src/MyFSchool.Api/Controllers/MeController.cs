using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.School;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.AuthenticatedSession)]
[Route("api/v1/me")]
public sealed class MeController(ISchoolScopeQueryService scopeService) : ControllerBase
{
    [HttpGet("classes")]
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

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);
}
