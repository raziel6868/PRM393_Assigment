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
    IAnnouncementEmailSender emailSender,
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
        else
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

        var parsedChannels = command.DeliveryChannels
            .Select(c => ParseChannel(c.Channel))
            .ToList();

        if (parsedChannels.Count == 0) return OperationResult.Fail("noChannels");
        if (parsedChannels.Any(channel => !channel.HasValue))
            return OperationResult.Fail("invalidChannel");

        var channels = parsedChannels
            .Select(channel => channel!.Value)
            .Distinct()
            .ToList();

        var recipients = await GetRecipientsAsync(post, ct);
        if (recipients.Count == 0) return OperationResult.Fail("noRecipients");
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var deliveries = new List<(AnnouncementDelivery Delivery, string? Email)>();
        foreach (var recipient in recipients)
        {
            foreach (var channel in channels)
            {
                var delivery = new AnnouncementDelivery
                {
                    Id = Guid.NewGuid(),
                    FeedPostId = post.Id,
                    RecipientUserId = recipient.UserId,
                    RecipientDisplayName = recipient.DisplayName,
                    Channel = channel,
                    Status = channel == DeliveryChannel.PortalApp
                        ? DeliveryStatus.Sent
                        : string.IsNullOrWhiteSpace(recipient.Email)
                            ? DeliveryStatus.Failed
                            : DeliveryStatus.Pending,
                    SentAtUtc = channel == DeliveryChannel.PortalApp ? now : null,
                    FailureReason = channel == DeliveryChannel.Email &&
                        string.IsNullOrWhiteSpace(recipient.Email)
                            ? "missingRecipientEmail"
                            : null
                };
                deliveries.Add((delivery, recipient.Email));
                dbContext.AnnouncementDeliveries.Add(delivery);
            }
        }

        post.IsPublished = true;
        post.PublishedAtUtc = now;
        dbContext.Entry(post).Property(item => item.RowVersion).OriginalValue = command.RowVersion;
        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult.Fail("concurrencyConflict");
        }

        foreach (var item in deliveries.Where(item =>
                     item.Delivery.Channel == DeliveryChannel.Email &&
                     item.Delivery.Status == DeliveryStatus.Pending))
        {
            var result = await emailSender.SendAsync(
                new AnnouncementEmail(item.Email!, post.Title, post.Body),
                ct);
            item.Delivery.Status = result.IsSuccess
                ? DeliveryStatus.Sent
                : DeliveryStatus.Failed;
            item.Delivery.SentAtUtc = result.IsSuccess
                ? timeProvider.GetUtcNow().UtcDateTime
                : null;
            item.Delivery.FailureReason = result.ErrorCode;
        }
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
                .Select(u => new RecipientInfo(u.Id, u.DisplayName ?? u.UserName ?? "", u.Email))
                .ToListAsync(ct),

            AnnouncementAudience.Student => await (from s in dbContext.StudentProfiles
                                                   join u in dbContext.Users on s.UserId equals u.Id
                                                   where s.IsActive && u.IsActive
                                                   select new RecipientInfo(u.Id, u.DisplayName ?? s.StudentCode, u.Email)).ToListAsync(ct),

            AnnouncementAudience.Parent => await (from p in dbContext.ParentProfiles
                                                 join u in dbContext.Users on p.UserId equals u.Id
                                                 where p.IsActive && u.IsActive
                                                 select new RecipientInfo(u.Id, u.DisplayName ?? p.ParentCode, u.Email)).ToListAsync(ct),

            AnnouncementAudience.Teacher => await (from t in dbContext.TeacherProfiles
                                                   join u in dbContext.Users on t.UserId equals u.Id
                                                   where t.IsActive && u.IsActive
                                                   select new RecipientInfo(u.Id, u.DisplayName ?? t.EmployeeCode, u.Email)).ToListAsync(ct),

            AnnouncementAudience.Class when post.TargetClassId.HasValue =>
                await GetClassRecipientsAsync(post.TargetClassId.Value, ct),

            _ => []
        };
    }

    private async Task<List<RecipientInfo>> GetClassRecipientsAsync(
        Guid classId,
        CancellationToken ct)
    {
        var students = await (
            from enrollment in dbContext.StudentEnrollments
            join student in dbContext.StudentProfiles on enrollment.StudentProfileId equals student.Id
            join user in dbContext.Users on student.UserId equals user.Id
            where enrollment.ClassId == classId && student.IsActive && user.IsActive
            select new RecipientInfo(
                user.Id,
                user.DisplayName ?? student.StudentCode,
                user.Email))
            .ToListAsync(ct);

        var parents = await (
            from enrollment in dbContext.StudentEnrollments
            join link in dbContext.ParentStudentLinks on enrollment.StudentProfileId equals link.StudentProfileId
            join parent in dbContext.ParentProfiles on link.ParentProfileId equals parent.Id
            join user in dbContext.Users on parent.UserId equals user.Id
            where enrollment.ClassId == classId &&
                  link.IsActive &&
                  parent.IsActive &&
                  user.IsActive
            select new RecipientInfo(
                user.Id,
                user.DisplayName ?? parent.ParentCode,
                user.Email))
            .ToListAsync(ct);

        return students
            .Concat(parents)
            .GroupBy(recipient => recipient.UserId)
            .Select(group => group.First())
            .ToList();
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

    private record RecipientInfo(Guid UserId, string DisplayName, string? Email);
}
