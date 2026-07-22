using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Api.Contracts.Relationships;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Parent)]
[Route("api/v1/relationships")]
public sealed class RelationshipsController(IRelationshipAuthorizationService relationshipService) : ControllerBase
{
    [HttpGet("children")]
    public async Task<IActionResult> GetChildren(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId))
        {
            return Unauthorized();
        }
        var children = await relationshipService.GetLinkedChildrenAsync(userId, cancellationToken);
        return Ok(children.Select(child => new LinkedChildResponse(
            child.StudentProfileId,
            child.UserId,
            child.DisplayName,
            child.StudentCode,
            ToWire(child.Relationship),
            child.IsPrimaryContact)).ToArray());
    }

    private static string ToWire(GuardianRelationship relationship) => relationship switch
    {
        GuardianRelationship.Father => "father",
        GuardianRelationship.Mother => "mother",
        GuardianRelationship.Guardian => "guardian",
        GuardianRelationship.Other => "other",
        _ => throw new ArgumentOutOfRangeException(nameof(relationship))
    };
}
