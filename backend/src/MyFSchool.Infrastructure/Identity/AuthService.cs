using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;
using MyFSchool.Infrastructure.Configuration;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Identity;

public sealed class AuthService(
    UserManager<AppUser> userManager,
    IPasswordHasher<AppUser> passwordHasher,
    MyFSchoolDbContext dbContext,
    IAccessTokenIssuer accessTokenIssuer,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider) : IAuthService
{
    private readonly AuthOptions _authOptions = authOptions.Value;
    private readonly string _dummyPasswordHash = passwordHasher.HashPassword(
        new AppUser { DisplayName = "Timing guard", UserName = "timing-guard" },
        "Synthetic-Timing-Guard-7!");

    public async Task<OperationResult<AuthSession>> SignInAsync(
        SignInCommand command,
        CancellationToken cancellationToken)
    {
        var identifier = command.EmailOrUserName.Trim();
        var user = await userManager.FindByEmailAsync(identifier) ?? await userManager.FindByNameAsync(identifier);
        if (user is null)
        {
            _ = passwordHasher.VerifyHashedPassword(
                new AppUser { DisplayName = "Timing guard", UserName = "timing-guard" },
                _dummyPasswordHash,
                command.Password);
            await AddAuditAsync("loginFailed", null, null, command.CorrelationId, cancellationToken);
            return OperationResult<AuthSession>.Failure("invalidCredentials");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await AddAuditAsync("loginFailed", user.Id, null, command.CorrelationId, cancellationToken);
            return OperationResult<AuthSession>.Failure("invalidCredentials");
        }

        var passwordMatches = await userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordMatches || !user.IsActive)
        {
            if (passwordMatches)
            {
                EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user));
            }
            else
            {
                EnsureSucceeded(await userManager.AccessFailedAsync(user));
            }
            await AddAuditAsync("loginFailed", user.Id, null, command.CorrelationId, cancellationToken);
            return OperationResult<AuthSession>.Failure("invalidCredentials");
        }

        var nowUtc = timeProvider.GetUtcNow();
        if (user.MustChangePassword &&
            (user.TemporaryPasswordExpiresAtUtc is null || user.TemporaryPasswordExpiresAtUtc <= nowUtc))
        {
            await AddAuditAsync("temporaryPasswordExpired", user.Id, user.Id, command.CorrelationId, cancellationToken);
            return OperationResult<AuthSession>.Failure("temporaryPasswordExpired");
        }

        EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user));
        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var passwordChangeRequired = user.MustChangePassword;
        var lifetime = TimeSpan.FromMinutes(
            passwordChangeRequired ? _authOptions.RestrictedTokenMinutes : _authOptions.AccessTokenMinutes);
        var accessToken = accessTokenIssuer.Issue(new AccessTokenDescriptor(
            user.Id,
            user.DisplayName,
            roles,
            passwordChangeRequired,
            lifetime));

        string? rawRefreshToken = null;
        DateTimeOffset? refreshExpiresAtUtc = null;
        if (!passwordChangeRequired)
        {
            rawRefreshToken = CreateRawRefreshToken();
            refreshExpiresAtUtc = nowUtc.AddDays(_authOptions.RefreshTokenDays);
            dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                FamilyId = Guid.NewGuid(),
                TokenHash = HashToken(rawRefreshToken),
                CreatedAtUtc = nowUtc,
                ExpiresAtUtc = refreshExpiresAtUtc.Value
            });
        }

        await AddAuditAsync("loginSucceeded", user.Id, user.Id, command.CorrelationId, cancellationToken, false);
        await dbContext.SaveChangesAsync(cancellationToken);
        return OperationResult<AuthSession>.Success(new AuthSession(
            user.Id,
            user.DisplayName,
            roles,
            passwordChangeRequired,
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            rawRefreshToken,
            refreshExpiresAtUtc));
    }

    public async Task<OperationResult<AuthSession>> RefreshAsync(
        RefreshSessionCommand command,
        CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var tokenHash = HashToken(command.RefreshToken);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var storedToken = await dbContext.RefreshTokens
            .FromSqlInterpolated($"SELECT * FROM [RefreshTokens] WITH (UPDLOCK, HOLDLOCK) WHERE [TokenHash] = {tokenHash}")
            .SingleOrDefaultAsync(cancellationToken);
        if (storedToken is null)
        {
            return OperationResult<AuthSession>.Failure("invalidRefreshToken");
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, nowUtc, "refreshTokenReuse", cancellationToken);
            await AddAuditAsync("refreshTokenReuse", storedToken.UserId, storedToken.UserId, command.CorrelationId, cancellationToken, false);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<AuthSession>.Failure("invalidRefreshToken");
        }

        if (!storedToken.IsActive(nowUtc))
        {
            storedToken.RevokedAtUtc = nowUtc;
            storedToken.RevocationReason = "expired";
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<AuthSession>.Failure("invalidRefreshToken");
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null || !user.IsActive || user.MustChangePassword)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, nowUtc, "accountUnavailable", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperationResult<AuthSession>.Failure("invalidRefreshToken");
        }

        var newRawToken = CreateRawRefreshToken();
        var newHash = HashToken(newRawToken);
        var newExpiresAtUtc = nowUtc.AddDays(_authOptions.RefreshTokenDays);
        storedToken.RevokedAtUtc = nowUtc;
        storedToken.RevocationReason = "rotated";
        storedToken.ReplacedByTokenHash = newHash;
        dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FamilyId = storedToken.FamilyId,
            TokenHash = newHash,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = newExpiresAtUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        var accessToken = accessTokenIssuer.Issue(new AccessTokenDescriptor(
            user.Id,
            user.DisplayName,
            roles,
            false,
            TimeSpan.FromMinutes(_authOptions.AccessTokenMinutes)));
        return OperationResult<AuthSession>.Success(new AuthSession(
            user.Id,
            user.DisplayName,
            roles,
            false,
            accessToken.Value,
            accessToken.ExpiresAtUtc,
            newRawToken,
            newExpiresAtUtc));
    }

    public async Task LogoutAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(command.RefreshToken);
        var storedToken = await dbContext.RefreshTokens.SingleOrDefaultAsync(
            token => token.TokenHash == tokenHash,
            cancellationToken);
        if (storedToken is null)
        {
            return;
        }

        var nowUtc = timeProvider.GetUtcNow();
        await RevokeFamilyAsync(storedToken.FamilyId, nowUtc, "logout", cancellationToken);
        await AddAuditAsync("logout", storedToken.UserId, storedToken.UserId, command.CorrelationId, cancellationToken, false);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionContext?> GetSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = (await userManager.GetRolesAsync(user)).ToArray();
        return new SessionContext(user.Id, user.DisplayName, roles, user.MustChangePassword);
    }

    private async Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset nowUtc,
        string reason,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = nowUtc;
            token.RevocationReason = reason;
        }
    }

    private async Task AddAuditAsync(
        string eventType,
        Guid? subjectUserId,
        Guid? actorUserId,
        string correlationId,
        CancellationToken cancellationToken,
        bool saveImmediately = true)
    {
        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            SubjectUserId = subjectUserId,
            ActorUserId = actorUserId,
            CorrelationId = correlationId,
            OccurredAtUtc = timeProvider.GetUtcNow()
        });
        if (saveImmediately)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static string CreateRawRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Unable to update account security state.");
        }
    }
}
