using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyFSchool.Application.Identity;
using MyFSchool.Infrastructure.Configuration;

namespace MyFSchool.Api.Identity;

public sealed class JwtAccessTokenIssuer(IOptions<AuthOptions> authOptions, TimeProvider timeProvider)
    : IAccessTokenIssuer
{
    public AccessToken Issue(AccessTokenDescriptor descriptor)
    {
        var options = authOptions.Value;
        var issuedAtUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = issuedAtUtc.Add(descriptor.Lifetime);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, descriptor.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, descriptor.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("passwordChangeRequired", descriptor.PasswordChangeRequired ? "true" : "false")
        };
        claims.AddRange(descriptor.Roles.Select(role => new Claim(ClaimTypes.Role, SchoolRoles.ToWire(role))));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            options.Issuer,
            options.Audience,
            claims,
            issuedAtUtc.UtcDateTime,
            expiresAtUtc.UtcDateTime,
            credentials);
        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
