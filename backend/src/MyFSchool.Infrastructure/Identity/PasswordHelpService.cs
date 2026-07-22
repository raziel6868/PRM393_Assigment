using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Domain.Identity;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Identity;

public sealed class PasswordHelpService(
    UserManager<AppUser> userManager,
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : IPasswordHelpService
{
    public async Task SubmitAsync(
        string emailOrUserName,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        try
        {
            var normalizedIdentifier = userManager.NormalizeName(emailOrUserName.Trim());
            var user = await dbContext.Users
                .OrderByDescending(candidate => candidate.NormalizedEmail == normalizedIdentifier)
                .FirstOrDefaultAsync(
                    candidate => candidate.NormalizedEmail == normalizedIdentifier ||
                                 candidate.NormalizedUserName == normalizedIdentifier,
                    cancellationToken);
            var subjectUserId = user?.IsActive == true ? user.Id : Guid.Empty;
            var pendingExists = await dbContext.PasswordHelpRequests.AnyAsync(
                request => request.UserId == subjectUserId && request.Status == PasswordHelpStatus.Pending,
                cancellationToken);
            if (user is null || !user.IsActive || pendingExists)
            {
                return;
            }

            var nowUtc = timeProvider.GetUtcNow();
            dbContext.PasswordHelpRequests.Add(new PasswordHelpRequest
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Status = PasswordHelpStatus.Pending,
                RequestedAtUtc = nowUtc
            });
            dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                EventType = "passwordHelpRequested",
                SubjectUserId = user.Id,
                ActorUserId = null,
                CorrelationId = correlationId,
                OccurredAtUtc = nowUtc
            });
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // A concurrent matching request won the filtered unique-index race.
            }
        }
        finally
        {
            var minimumDuration = TimeSpan.FromMilliseconds(RandomNumberGenerator.GetInt32(300, 351));
            var remaining = minimumDuration - Stopwatch.GetElapsedTime(started);
            if (remaining > TimeSpan.Zero)
            {
                await Task.Delay(remaining, cancellationToken);
            }
        }
    }
}
