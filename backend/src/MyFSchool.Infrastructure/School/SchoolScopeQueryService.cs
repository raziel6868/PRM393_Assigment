using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class SchoolScopeQueryService(MyFSchoolDbContext dbContext)
    : ISchoolScopeQueryService
{
    public async Task<IReadOnlyList<StudentClassScope>> GetStudentClassesAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await (
            from profile in dbContext.StudentProfiles.AsNoTracking()
            join enrollment in dbContext.StudentEnrollments.AsNoTracking() on profile.Id equals enrollment.StudentProfileId
            join classroom in dbContext.ClassRooms.AsNoTracking() on enrollment.ClassId equals classroom.Id
            join year in dbContext.SchoolYears.AsNoTracking() on classroom.SchoolYearId equals year.Id
            where profile.UserId == userId && profile.IsActive && classroom.IsActive && year.IsActive
            orderby classroom.Code, classroom.Id
            select new StudentClassScope(
                classroom.Id,
                classroom.Code,
                classroom.DisplayName,
                year.Id,
                year.Code))
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TeacherClassScope>> GetTeacherClassesAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await (
            from profile in dbContext.TeacherProfiles.AsNoTracking()
            join assignment in dbContext.TeacherClassSubjectAssignments.AsNoTracking() on profile.Id equals assignment.TeacherProfileId
            join classroom in dbContext.ClassRooms.AsNoTracking() on assignment.ClassId equals classroom.Id
            join subject in dbContext.Subjects.AsNoTracking() on assignment.SubjectId equals subject.Id
            join year in dbContext.SchoolYears.AsNoTracking() on assignment.SchoolYearId equals year.Id
            where profile.UserId == userId && profile.IsActive &&
                  assignment.IsActive && classroom.IsActive && subject.IsActive && year.IsActive
            orderby year.Code descending, classroom.Code, subject.Code
            select new TeacherClassScope(
                classroom.Id,
                classroom.Code,
                classroom.DisplayName,
                subject.Id,
                subject.Code,
                subject.DisplayName,
                year.Id,
                year.Code,
                classroom.HomeroomTeacherProfileId == profile.Id))
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ParentChildClassScope>> GetParentChildrenClassesAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        await (
            from parent in dbContext.ParentProfiles.AsNoTracking()
            join link in dbContext.ParentStudentLinks.AsNoTracking() on parent.Id equals link.ParentProfileId
            join student in dbContext.StudentProfiles.AsNoTracking() on link.StudentProfileId equals student.Id
            join studentUser in dbContext.Users.AsNoTracking() on student.UserId equals studentUser.Id
            join enrollment in dbContext.StudentEnrollments.AsNoTracking() on student.Id equals enrollment.StudentProfileId
            join classroom in dbContext.ClassRooms.AsNoTracking() on enrollment.ClassId equals classroom.Id
            join year in dbContext.SchoolYears.AsNoTracking() on classroom.SchoolYearId equals year.Id
            where parent.UserId == userId && parent.IsActive &&
                  link.IsActive && student.IsActive && classroom.IsActive && year.IsActive
            orderby student.StudentCode, classroom.Code
            select new ParentChildClassScope(
                student.Id,
                student.UserId,
                studentUser.DisplayName,
                student.StudentCode,
                classroom.Id,
                classroom.Code,
                classroom.DisplayName,
                year.Id,
                year.Code))
        .ToListAsync(cancellationToken);
}
