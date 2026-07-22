using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class AnnouncementQueryService(MyFSchoolDbContext dbContext) : IAnnouncementQueryService
{
    public async Task<AnnouncementPage> GetForUserAsync(
        Guid userId,
        string userRole,
        Guid? userProfileId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize < 1 ? 20 : Math.Min(pageSize, 100);

        var query = dbContext.FeedPosts
            .Include(p => p.TargetClass)
            .Where(p => p.IsPublished);

        query = ApplyAudienceFilter(query, userId, userRole, userProfileId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.PublishedAtUtc ?? p.CreatedAtUtc)
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .Select(p => new AnnouncementListItem(
                p.Id,
                p.Title,
                TruncateBody(p.Body),
                p.Audience.ToString(),
                p.TargetClass != null ? p.TargetClass.DisplayName : null,
                p.AuthorDisplayName,
                p.CreatedAtUtc,
                p.PublishedAtUtc,
                p.ImageUrl,
                p.ReadStates.Count,
                p.Deliveries.Count))
            .ToListAsync(ct);

        return new AnnouncementPage(items, boundedPage, boundedPageSize, totalCount,
            (int)Math.Ceiling((double)totalCount / boundedPageSize));
    }

    public async Task<OperationResult<AnnouncementDetail>> GetDetailForUserAsync(
        Guid announcementId,
        Guid userId,
        string userRole,
        Guid? userProfileId,
        CancellationToken ct = default)
    {
        var post = await dbContext.FeedPosts
            .Include(p => p.TargetClass)
            .Include(p => p.ReadStates)
            .Include(p => p.Deliveries)
            .FirstOrDefaultAsync(p => p.Id == announcementId, ct);

        if (post == null) return OperationResult<AnnouncementDetail>.Failure("notFound");

        if (!post.IsPublished) return OperationResult<AnnouncementDetail>.Failure("notPublished");

        if (!await CanViewAsync(post, userId, userRole, userProfileId, ct))
            return OperationResult<AnnouncementDetail>.Failure("forbidden");

        return OperationResult<AnnouncementDetail>.Success(MapToDetail(post));
    }

    public async Task<OperationResult> MarkAsReadAsync(Guid announcementId, Guid userId, CancellationToken ct = default)
    {
        var post = await dbContext.FeedPosts.FindAsync([announcementId], ct);
        if (post == null) return OperationResult.Fail("notFound");

        if (!post.IsPublished) return OperationResult.Fail("notPublished");

        var existing = await dbContext.AnnouncementReadStates
            .AnyAsync(s => s.FeedPostId == announcementId && s.UserId == userId, ct);

        if (!existing)
        {
            dbContext.AnnouncementReadStates.Add(new AnnouncementReadState
            {
                Id = Guid.NewGuid(),
                FeedPostId = announcementId,
                UserId = userId,
                ReadAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync(ct);
        }

        return OperationResult.Ok();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        var readPostIds = await dbContext.AnnouncementReadStates
            .Where(s => s.UserId == userId)
            .Select(s => s.FeedPostId)
            .ToListAsync(ct);

        return await dbContext.FeedPosts
            .Where(p => p.IsPublished && !readPostIds.Contains(p.Id))
            .CountAsync(ct);
    }

    private static string TruncateBody(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Length <= 200) return body;
        return body[..200] + "...";
    }

    private IQueryable<FeedPost> ApplyAudienceFilter(
        IQueryable<FeedPost> query,
        Guid userId,
        string userRole,
        Guid? userProfileId)
    {
        var role = userRole.ToLowerInvariant();

        return role switch
        {
            "administrator" => query,
            "teacher" when userProfileId.HasValue => query.Where(p =>
                p.Audience == AnnouncementAudience.SchoolWide ||
                p.Audience == AnnouncementAudience.Teacher ||
                (p.Audience == AnnouncementAudience.Class && p.TargetClassId != null &&
                 dbContext.TeacherClassSubjectAssignments.Any(a =>
                     a.TeacherProfileId == userProfileId.Value && a.ClassId == p.TargetClassId))),
            "student" when userProfileId.HasValue => query.Where(p =>
                p.Audience == AnnouncementAudience.SchoolWide ||
                p.Audience == AnnouncementAudience.Student ||
                (p.Audience == AnnouncementAudience.Class && p.TargetClassId != null &&
                 dbContext.StudentEnrollments.Any(e =>
                     e.StudentProfileId == userProfileId.Value &&
                     e.ClassId == p.TargetClassId))),
            "parent" when userProfileId.HasValue => query.Where(p =>
                p.Audience == AnnouncementAudience.SchoolWide ||
                p.Audience == AnnouncementAudience.Parent ||
                (p.Audience == AnnouncementAudience.Class && p.TargetClassId != null &&
                 dbContext.ParentStudentLinks.Any(l =>
                     l.ParentProfileId == userProfileId.Value &&
                     l.IsActive &&
                     dbContext.StudentEnrollments.Any(e =>
                         e.StudentProfileId == l.StudentProfileId &&
                         e.ClassId == p.TargetClassId)))),
            _ => query.Where(p => false)
        };
    }

    private async Task<bool> CanViewAsync(
        FeedPost post,
        Guid userId,
        string userRole,
        Guid? userProfileId,
        CancellationToken ct)
    {
        var role = userRole.ToLowerInvariant();

        return role switch
        {
            "administrator" => true,
            "teacher" when userProfileId.HasValue => post.Audience switch
            {
                AnnouncementAudience.SchoolWide => true,
                AnnouncementAudience.Teacher => true,
                AnnouncementAudience.Class when post.TargetClassId.HasValue => await dbContext.TeacherClassSubjectAssignments
                    .AnyAsync(a => a.TeacherProfileId == userProfileId.Value && a.ClassId == post.TargetClassId, ct),
                _ => false
            },
            "student" when userProfileId.HasValue => post.Audience switch
            {
                AnnouncementAudience.SchoolWide => true,
                AnnouncementAudience.Student => true,
                AnnouncementAudience.Class when post.TargetClassId.HasValue => await dbContext.StudentEnrollments
                    .AnyAsync(e => e.StudentProfileId == userProfileId.Value &&
                                   e.ClassId == post.TargetClassId, ct),
                _ => false
            },
            "parent" when userProfileId.HasValue => post.Audience switch
            {
                AnnouncementAudience.SchoolWide => true,
                AnnouncementAudience.Parent => true,
                AnnouncementAudience.Class when post.TargetClassId.HasValue => await dbContext.ParentStudentLinks
                    .AnyAsync(l =>
                        l.ParentProfileId == userProfileId.Value && l.IsActive &&
                        dbContext.StudentEnrollments.Any(e =>
                            e.StudentProfileId == l.StudentProfileId &&
                            e.ClassId == post.TargetClassId), ct),
                _ => false
            },
            _ => false
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
}
