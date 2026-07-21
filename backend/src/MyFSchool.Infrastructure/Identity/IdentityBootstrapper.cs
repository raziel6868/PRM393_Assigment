using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;
using MyFSchool.Infrastructure.Configuration;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Identity;

public sealed class IdentityBootstrapper(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    MyFSchoolDbContext dbContext,
    IOptions<BootstrapOptions> bootstrapOptions,
    TimeProvider timeProvider) : IIdentityBootstrapper
{
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var options = bootstrapOptions.Value;
        if (!options.Enabled)
        {
            return;
        }

        foreach (var roleName in SchoolRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new AppRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName
                });
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to initialize required school roles.");
                }
            }
        }

        var existing = await userManager.FindByNameAsync(options.AdministratorUserName);
        if (existing is not null)
        {
            if (!existing.LockoutEnabled)
            {
                existing.LockoutEnabled = true;
                var updateResult = await userManager.UpdateAsync(existing);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to update the configured Administrator account.");
                }
            }
            if (!await userManager.IsInRoleAsync(existing, SchoolRoles.Administrator))
            {
                var roleResult = await userManager.AddToRoleAsync(existing, SchoolRoles.Administrator);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Unable to initialize the Administrator role assignment.");
                }
            }
            return;
        }

        var nowUtc = timeProvider.GetUtcNow();
        var administrator = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = options.AdministratorUserName,
            Email = options.AdministratorEmail,
            EmailConfirmed = true,
            DisplayName = options.AdministratorDisplayName,
            IsActive = true,
            LockoutEnabled = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
        var createResult = await userManager.CreateAsync(administrator, options.AdministratorPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException("Unable to initialize the configured Administrator account.");
        }
        var addRoleResult = await userManager.AddToRoleAsync(administrator, SchoolRoles.Administrator);
        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException("Unable to initialize the Administrator role assignment.");
        }

        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "administratorBootstrapped",
            SubjectUserId = administrator.Id,
            ActorUserId = administrator.Id,
            CorrelationId = "bootstrap",
            OccurredAtUtc = nowUtc
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
