using Microsoft.AspNetCore.Identity;
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
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            DisplayName = command.DisplayName.Trim(),
            UserName = command.UserName.Trim(),
            Email = string.IsNullOrWhiteSpace(command.Email) ? null : command.Email.Trim(),
            EmailConfirmed = !string.IsNullOrWhiteSpace(command.Email),
            IsActive = true,
            LockoutEnabled = true,
            MustChangePassword = true,
            TemporaryPasswordExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        var temporaryPassword = PasswordGenerator.Generate();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var creation = await userManager.CreateAsync(user, temporaryPassword);
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
}
