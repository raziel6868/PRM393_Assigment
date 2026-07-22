using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Identity;

public sealed class RelationshipAuthorizationService(MyFSchoolDbContext dbContext)
    : IRelationshipAuthorizationService
{
    public async Task<IReadOnlyList<LinkedChild>> GetLinkedChildrenAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await (
            from parent in dbContext.ParentProfiles.AsNoTracking()
            join parentUser in dbContext.Users.AsNoTracking() on parent.UserId equals parentUser.Id
            join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
            join student in dbContext.StudentProfiles.AsNoTracking() on link.StudentProfileId equals student.Id
            join studentUser in dbContext.Users.AsNoTracking() on student.UserId equals studentUser.Id
            where parent.UserId == userId && parent.IsActive && parentUser.IsActive &&
                  link.IsActive && student.IsActive && studentUser.IsActive
            orderby student.StudentCode, student.Id
            select new LinkedChild(
                student.Id,
                student.UserId,
                studentUser.DisplayName,
                student.StudentCode,
                link.Relationship,
                link.IsPrimaryContact))
        .ToListAsync(cancellationToken);

    public Task<bool> CanAccessStudentAsync(
        Guid userId,
        Guid studentProfileId,
        CancellationToken cancellationToken) =>
        (
            from student in dbContext.StudentProfiles.AsNoTracking()
            join studentUser in dbContext.Users.AsNoTracking() on student.UserId equals studentUser.Id
            where student.Id == studentProfileId && student.IsActive && studentUser.IsActive &&
                  (student.UserId == userId ||
                   (from parent in dbContext.ParentProfiles.AsNoTracking()
                    join parentUser in dbContext.Users.AsNoTracking() on parent.UserId equals parentUser.Id
                    join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
                    where parent.UserId == userId && parent.IsActive && parentUser.IsActive &&
                          link.StudentProfileId == student.Id && link.IsActive
                    select link.Id).Any())
            select student.Id)
        .AnyAsync(cancellationToken);
}
