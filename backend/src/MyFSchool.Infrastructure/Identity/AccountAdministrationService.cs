using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;
using MyFSchool.Infrastructure.Configuration;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Identity;

public sealed class AccountAdministrationService(
    UserManager<AppUser> userManager,
    MyFSchoolDbContext dbContext,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider) : IAccountAdministrationService
{
    public async Task<OperationResult<ProvisionedUser>> ProvisionAsync(
        ProvisionUserCommand command,
        CancellationToken cancellationToken)
    {
        var roles = command.Roles
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (roles.Length == 0 || roles.Any(role => !SchoolRoles.All.Contains(role)))
        {
            return OperationResult<ProvisionedUser>.Failure("invalidRoles");
        }

        var nowUtc = timeProvider.GetUtcNow();
        var expiresAtUtc = nowUtc.AddHours(authOptions.Value.TemporaryPasswordHours);
        var normalizedEmail = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim();
        if (normalizedEmail is not null && await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return OperationResult<ProvisionedUser>.Failure("accountAlreadyExists");
        }
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            DisplayName = command.DisplayName.Trim(),
            UserName = command.UserName.Trim(),
            Email = normalizedEmail,
            EmailConfirmed = normalizedEmail is not null,
            IsActive = true,
            LockoutEnabled = true,
            MustChangePassword = true,
            TemporaryPasswordExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        var temporaryPassword = PasswordGenerator.Generate();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        IdentityResult creation;
        try
        {
            creation = await userManager.CreateAsync(user, temporaryPassword);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return OperationResult<ProvisionedUser>.Failure("accountAlreadyExists");
        }
        if (!creation.Succeeded)
        {
            return OperationResult<ProvisionedUser>.Failure(
                creation.Errors.Any(error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
                    ? "accountAlreadyExists"
                    : "accountValidationFailed");
        }

        var roleResult = await userManager.AddToRolesAsync(user, roles);
        if (!roleResult.Succeeded)
        {
            return OperationResult<ProvisionedUser>.Failure("invalidRoles");
        }

        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "accountProvisioned",
            SubjectUserId = user.Id,
            ActorUserId = command.ActorUserId,
            CorrelationId = command.CorrelationId,
            OccurredAtUtc = nowUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<ProvisionedUser>.Success(new ProvisionedUser(
            user.Id,
            user.DisplayName,
            user.UserName!,
            user.Email,
            roles,
            temporaryPassword,
            expiresAtUtc));
    }

    public async Task<PasswordHelpPage> GetPasswordHelpRequestsAsync(
        PasswordHelpQuery query,
        CancellationToken cancellationToken)
    {
        var status = ToDomainStatus(query.Status);
        var source =
            from request in dbContext.PasswordHelpRequests.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on request.UserId equals user.Id
            where request.Status == status
            orderby request.RequestedAtUtc, request.Id
            select new { request, user };
        var totalCount = await source.CountAsync(cancellationToken);
        var rows = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
        var items = rows.Select(row => new PasswordHelpItem(
            row.request.Id,
            row.user.Id,
            row.user.DisplayName,
            row.user.UserName!,
            row.user.Email,
            query.Status,
            row.request.RequestedAtUtc,
            row.request.ResolvedAtUtc,
            Convert.ToBase64String(row.request.RowVersion))).ToArray();
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize);
        return new PasswordHelpPage(items, query.Page, query.PageSize, totalCount, totalPages);
    }

    public async Task<OperationResult<IssuedTemporaryPassword>> IssueTemporaryPasswordAsync(
        IssueTemporaryPasswordCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.Confirmed)
        {
            return OperationResult<IssuedTemporaryPassword>.Failure("confirmationRequired");
        }

        var nowUtc = timeProvider.GetUtcNow();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var activeTokens = await dbContext.RefreshTokens
            .FromSqlInterpolated($"SELECT * FROM [RefreshTokens] WITH (UPDLOCK, HOLDLOCK) WHERE [UserId] = {command.UserId} AND [RevokedAtUtc] IS NULL")
            .ToListAsync(cancellationToken);
        var pendingRequest = await dbContext.PasswordHelpRequests
            .FromSqlInterpolated($"SELECT * FROM [PasswordHelpRequests] WITH (UPDLOCK, HOLDLOCK) WHERE [UserId] = {command.UserId} AND [Status] = 0")
            .SingleOrDefaultAsync(cancellationToken);
        if (pendingRequest is null || !RowVersionMatches(command.RequestRowVersion, pendingRequest.RowVersion))
        {
            return OperationResult<IssuedTemporaryPassword>.Failure("passwordHelpRequestNotPending");
        }
        var user = await userManager.FindByIdAsync(command.UserId.ToString());
        if (user is null || !user.IsActive)
        {
            return OperationResult<IssuedTemporaryPassword>.Failure("accountNotFound");
        }

        var temporaryPassword = PasswordGenerator.Generate();
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, temporaryPassword);
        if (!resetResult.Succeeded)
        {
            if (resetResult.Errors.Any(error => error.Code == "ConcurrencyFailure"))
            {
                return OperationResult<IssuedTemporaryPassword>.Failure("passwordHelpRequestNotPending");
            }
            throw new InvalidOperationException("Unable to issue a temporary password.");
        }

        var expiresAtUtc = nowUtc.AddHours(authOptions.Value.TemporaryPasswordHours);
        user.MustChangePassword = true;
        user.TemporaryPasswordExpiresAtUtc = expiresAtUtc;
        user.UpdatedAtUtc = nowUtc;
        EnsureSucceeded(await userManager.UpdateAsync(user), "Unable to update the temporary-password state.");
        EnsureSucceeded(await userManager.SetLockoutEndDateAsync(user, null), "Unable to clear the account lockout.");
        EnsureSucceeded(await userManager.ResetAccessFailedCountAsync(user), "Unable to clear failed sign-in attempts.");

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = nowUtc;
            token.RevocationReason = "temporaryPasswordIssued";
        }
        if (pendingRequest is not null)
        {
            pendingRequest.Status = PasswordHelpStatus.Resolved;
            pendingRequest.ResolvedAtUtc = nowUtc;
            pendingRequest.ResolvedByUserId = command.ActorUserId;
        }
        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "temporaryPasswordIssued",
            SubjectUserId = user.Id,
            ActorUserId = command.ActorUserId,
            CorrelationId = command.CorrelationId,
            OccurredAtUtc = nowUtc
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<IssuedTemporaryPassword>.Failure("passwordHelpRequestNotPending");
        }
        return OperationResult<IssuedTemporaryPassword>.Success(new IssuedTemporaryPassword(
            user.Id,
            temporaryPassword,
            expiresAtUtc));
    }

    public async Task<OperationResult<bool>> RejectPasswordHelpRequestAsync(
        RejectPasswordHelpCommand command,
        CancellationToken cancellationToken)
    {
        if (!command.Confirmed)
        {
            return OperationResult<bool>.Failure("confirmationRequired");
        }
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var request = await dbContext.PasswordHelpRequests
            .FromSqlInterpolated($"SELECT * FROM [PasswordHelpRequests] WITH (UPDLOCK, HOLDLOCK) WHERE [Id] = {command.RequestId}")
            .SingleOrDefaultAsync(cancellationToken);
        if (request is null || request.Status != PasswordHelpStatus.Pending ||
            !RowVersionMatches(command.RequestRowVersion, request.RowVersion))
        {
            return OperationResult<bool>.Failure("passwordHelpRequestNotPending");
        }

        var nowUtc = timeProvider.GetUtcNow();
        request.Status = PasswordHelpStatus.Rejected;
        request.ResolvedAtUtc = nowUtc;
        request.ResolvedByUserId = command.ActorUserId;
        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "passwordHelpRejected",
            SubjectUserId = request.UserId,
            ActorUserId = command.ActorUserId,
            CorrelationId = command.CorrelationId,
            OccurredAtUtc = nowUtc
        });
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<bool>.Failure("passwordHelpRequestNotPending");
        }
        return OperationResult<bool>.Success(true);
    }

    private static PasswordHelpStatus ToDomainStatus(PasswordHelpStatusFilter status) => status switch
    {
        PasswordHelpStatusFilter.Pending => PasswordHelpStatus.Pending,
        PasswordHelpStatusFilter.Resolved => PasswordHelpStatus.Resolved,
        PasswordHelpStatusFilter.Rejected => PasswordHelpStatus.Rejected,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static bool RowVersionMatches(byte[] supplied, byte[] persisted) =>
        supplied.Length == persisted.Length && CryptographicOperations.FixedTimeEquals(supplied, persisted);

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(message);
        }
    }
}
