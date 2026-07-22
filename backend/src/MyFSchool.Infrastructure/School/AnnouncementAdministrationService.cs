using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Identity;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class AnnouncementAdministrationService(
    MyFSchoolDbContext dbContext,
    UserManager<AppUser> userManager,
    TimeProvider timeProvider) : IAnnouncementAdministrationService
{
    public async Task<OperationResult<AnnouncementDetail>> CreateAsync(
        CreateAnnouncementCommand command,
        Guid authorUserId,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(authorUserId.ToString());
        if (user == null) return OperationResult<AnnouncementDetail>.Failure("unauthorized");

        var audience = ParseAudience(command.Audience);
        if (audience == null) return OperationResult<AnnouncementDetail>.Failure("invalidAudience");

        Guid? targetClassId = null;
        if (audience == AnnouncementAudience.Class)
        {
            if (!command.TargetClassId.HasValue)
                return OperationResult<AnnouncementDetail>.Failure("targetClassRequired");

            targetClassId = command.TargetClassId.Value;
            if (!await IsTeacherOfClassAsync(authorUserId, targetClassId.Value, ct))
                return OperationResult<AnnouncementDetail>.Failure("forbidden");
        }
        else if (audience == AnnouncementAudience.SchoolWide)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("Administrator"))
                return OperationResult<AnnouncementDetail>.Failure("forbidden");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var post = new FeedPost
        {
            Id = Guid.NewGuid(),
            Title = command.Title.Trim(),
            Body = command.Body.Trim(),
            Audience = audience.Value,
            TargetClassId = targetClassId,
            AuthorUserId = authorUserId,
            AuthorDisplayName = user.DisplayName ?? user.UserName ?? "Unknown",
            IsPublished = false,
            CreatedAtUtc = now,
            ImageUrl = command.ImageUrl
        };

        dbContext.FeedPosts.Add(post);
        await dbContext.SaveChangesAsync(ct);

        return OperationResult<AnnouncementDetail>.Success(MapToDetail(post));
    }

    public async Task<OperationResult<AnnouncementDetail>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var post = await dbContext.FeedPosts
            .Include(p => p.TargetClass)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (post == null) return OperationResult<AnnouncementDetail>.Failure("notFound");

        return OperationResult<AnnouncementDetail>.Success(MapToDetail(post));
    }

    public async Task<OperationResult> PublishAsync(
        PublishAnnouncementCommand command,
        Guid authorUserId,
        CancellationToken ct = default)
    {
        var post = await dbContext.FeedPosts.FirstOrDefaultAsync(p => p.Id == command.Id, ct);
        if (post == null) return OperationResult.Fail("notFound");

        if (post.AuthorUserId != authorUserId)
        {
            var user = await userManager.FindByIdAsync(authorUserId.ToString());
            if (user == null) return OperationResult.Fail("unauthorized");
            var roles = await userManager.GetRolesAsync(user);
            if (!roles.Contains("Administrator"))
                return OperationResult.Fail("forbidden");
        }

        if (post.IsPublished) return OperationResult.Fail("alreadyPublished");

        var channels = command.DeliveryChannels
            .Select(c => ParseChannel(c.Channel))
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();

        if (channels.Count == 0) return OperationResult.Fail("noChannels");

        var recipients = await GetRecipientsAsync(post, ct);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var recipient in recipients)
        {
            foreach (var channel in channels)
            {
                dbContext.AnnouncementDeliveries.Add(new AnnouncementDelivery
                {
                    Id = Guid.NewGuid(),
                    FeedPostId = post.Id,
                    RecipientUserId = recipient.UserId,
                    RecipientDisplayName = recipient.DisplayName,
                    Channel = channel,
                    Status = DeliveryStatus.Pending
                });
            }
        }

        post.IsPublished = true;
        post.PublishedAtUtc = now;

        await dbContext.SaveChangesAsync(ct);
        return OperationResult.Ok();
    }

    private async Task<bool> IsTeacherOfClassAsync(Guid userId, Guid classId, CancellationToken ct)
    {
        var teacherProfile = await dbContext.TeacherProfiles
            .FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (teacherProfile == null) return false;

        return await dbContext.TeacherClassSubjectAssignments
            .AnyAsync(a => a.TeacherProfileId == teacherProfile.Id && a.ClassId == classId, ct);
    }

    private static AnnouncementAudience? ParseAudience(string value) => value.ToLowerInvariant() switch
    {
        "schoolwide" => AnnouncementAudience.SchoolWide,
        "class" => AnnouncementAudience.Class,
        "teacher" => AnnouncementAudience.Teacher,
        "parent" => AnnouncementAudience.Parent,
        "student" => AnnouncementAudience.Student,
        _ => null
    };

    private static DeliveryChannel? ParseChannel(string value) => value.ToLowerInvariant() switch
    {
        "portalapp" => DeliveryChannel.PortalApp,
        "email" => DeliveryChannel.Email,
        _ => null
    };

    private async Task<List<RecipientInfo>> GetRecipientsAsync(FeedPost post, CancellationToken ct)
    {
        return post.Audience switch
        {
            AnnouncementAudience.SchoolWide => await dbContext.Users
                .Where(u => u.IsActive && !u.MustChangePassword)
                .Select(u => new RecipientInfo(u.Id, u.DisplayName ?? u.UserName ?? ""))
                .ToListAsync(ct),

            AnnouncementAudience.Student => await (from s in dbContext.StudentProfiles
                                                   join u in dbContext.Users on s.UserId equals u.Id
                                                   where s.IsActive && u.IsActive
                                                   select new RecipientInfo(u.Id, u.DisplayName ?? s.StudentCode)).ToListAsync(ct),

            AnnouncementAudience.Parent => await (from p in dbContext.ParentProfiles
                                                 join u in dbContext.Users on p.UserId equals u.Id
                                                 where p.IsActive && u.IsActive
                                                 select new RecipientInfo(u.Id, u.DisplayName ?? p.ParentCode)).ToListAsync(ct),

            AnnouncementAudience.Teacher => await (from t in dbContext.TeacherProfiles
                                                   join u in dbContext.Users on t.UserId equals u.Id
                                                   where t.IsActive && u.IsActive
                                                   select new RecipientInfo(u.Id, u.DisplayName ?? t.EmployeeCode)).ToListAsync(ct),

            _ => []
        };
    }

    private AnnouncementDetail MapToDetail(FeedPost post) => new(
        post.Id,
        post.Title,
        post.Body,
        post.Audience.ToString(),
        post.TargetClass?.DisplayName,
        post.AuthorDisplayName,
        post.CreatedAtUtc,
        post.PublishedAtUtc,
        post.ImageUrl,
        post.ReadStates.Count,
        post.Deliveries.Count,
        post.RowVersion);

    private record RecipientInfo(Guid UserId, string DisplayName);
}
