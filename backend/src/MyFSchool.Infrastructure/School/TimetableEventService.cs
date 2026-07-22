using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Infrastructure.Persistence;
using DomainSchool = MyFSchool.Domain.School;

namespace MyFSchool.Infrastructure.School;

public sealed class TimetableQueryService(MyFSchoolDbContext dbContext) : ITimetableQueryService
{
    public async Task<IReadOnlyList<TimetableEntryResult>> GetWeekTimetableAsync(
        Guid userId,
        string role,
        Guid? studentProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken)
    {
        IQueryable<DomainSchool.TimetableEntry> query = dbContext.TimetableEntries.AsNoTracking()
            .Where(e => e.IsActive);

        if (role == "student")
        {
            var profileId = studentProfileId ?? await dbContext.StudentProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken) ?? Guid.Empty;

            var classIds = await dbContext.StudentEnrollments
                .Where(e => e.StudentProfileId == profileId)
                .Select(e => e.ClassId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(e => classIds.Contains(e.ClassId));
        }
        else if (role == "teacher")
        {
            var profileId = await dbContext.TeacherProfiles
                .Where(p => p.UserId == userId && p.IsActive)
                .Select(p => (Guid?)p.Id)
                .FirstOrDefaultAsync(cancellationToken) ?? Guid.Empty;

            var classIds = await dbContext.TeacherClassSubjectAssignments
                .Where(a => a.TeacherProfileId == profileId && a.IsActive)
                .Select(a => a.ClassId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(e => classIds.Contains(e.ClassId));
        }
        else if (role == "parent" && studentProfileId.HasValue)
        {
            var classIds = await dbContext.StudentEnrollments
                .Where(e => e.StudentProfileId == studentProfileId.Value)
                .Select(e => e.ClassId)
                .Distinct()
                .ToListAsync(cancellationToken);

            query = query.Where(e => classIds.Contains(e.ClassId));
        }

        var entries = await (from entry in query
            join classroom in dbContext.ClassRooms.AsNoTracking() on entry.ClassId equals classroom.Id
            join subject in dbContext.Subjects.AsNoTracking() on entry.SubjectId equals subject.Id
            join teacher in dbContext.TeacherProfiles.AsNoTracking() on entry.TeacherProfileId equals teacher.Id
            join teacherUser in dbContext.Users.AsNoTracking() on teacher.UserId equals teacherUser.Id
            join year in dbContext.SchoolYears.AsNoTracking() on entry.SchoolYearId equals year.Id
            where year.IsActive
            orderby entry.DayOfWeek, entry.StartTime
            select new
            {
                entry,
                ClassCode = classroom.Code,
                ClassName = classroom.DisplayName,
                SubjectCode = subject.Code,
                SubjectName = subject.DisplayName,
                TeacherName = teacherUser.DisplayName,
                YearCode = year.Code
            }).ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        return entries.Select(e =>
        {
            var state = e.entry.DayOfWeek < (int)today.DayOfWeek ? "past"
                : e.entry.DayOfWeek > (int)today.DayOfWeek ? "upcoming"
                : currentTime < e.entry.StartTime ? "upcoming"
                : currentTime <= e.entry.EndTime ? "ongoing"
                : "past";

            return new TimetableEntryResult(
                e.entry.Id, e.entry.ClassId, e.ClassCode, e.ClassName,
                e.entry.SubjectId, e.SubjectCode, e.SubjectName,
                e.entry.TeacherProfileId, e.TeacherName,
                e.entry.SchoolYearId, e.entry.DayOfWeek,
                e.entry.StartTime.ToString("HH:mm:ss"),
                e.entry.EndTime.ToString("HH:mm:ss"),
                e.entry.Room, state);
        }).ToList();
    }
}

public sealed class EventQueryService(MyFSchoolDbContext dbContext) : IEventQueryService
{
    private const int MaxPageSize = 100;

    public async Task<OperationResult<EventPageResult>> GetUpcomingEventsAsync(
        Guid? userId,
        string? role,
        Guid? studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize switch { < 1 => 20, > MaxPageSize => MaxPageSize, _ => pageSize };

        var query = dbContext.SchoolEvents.AsNoTracking()
            .Where(e => e.IsActive && e.EndAtUtc >= DateTime.UtcNow);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)boundedPageSize);

        var events = await query
            .OrderBy(e => e.StartAtUtc)
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var items = events.Select(e =>
        {
            var status = e.EndAtUtc < now ? "ended"
                : e.StartAtUtc <= now ? "ongoing"
                : "upcoming";
            return new EventResult(e.Id, e.Title, e.Description, e.StartAtUtc, e.EndAtUtc,
                e.Location, e.OrganizerContact, e.Audience, status);
        }).ToList();

        return OperationResult<EventPageResult>.Success(new EventPageResult(items, boundedPage, boundedPageSize, totalCount, totalPages));
    }

    public async Task<OperationResult<EventResult>> GetEventDetailAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var evt = await dbContext.SchoolEvents.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == eventId, cancellationToken);
        if (evt is null) return OperationResult<EventResult>.Failure("eventNotFound");

        var now = DateTime.UtcNow;
        var status = evt.EndAtUtc < now ? "ended" : evt.StartAtUtc <= now ? "ongoing" : "upcoming";
        return OperationResult<EventResult>.Success(new EventResult(
            evt.Id, evt.Title, evt.Description, evt.StartAtUtc, evt.EndAtUtc,
            evt.Location, evt.OrganizerContact, evt.Audience, status));
    }
}
