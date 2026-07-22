using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Domain.Identity;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.School;

public sealed class AttendanceAdministrationService(
    MyFSchoolDbContext dbContext,
    TimeProvider timeProvider) : IAttendanceAdministrationService
{
    private const int MaxPageSize = 100;

    public async Task<OperationResult<AttendanceRosterResult>> GetClassRosterAsync(
        Guid actorUserId,
        Guid classId,
        DateOnly attendanceDate,
        SchoolSession session,
        CancellationToken cancellationToken)
    {
        if (!await IsTeacherAssignedToClassAsync(actorUserId, classId, cancellationToken))
        {
            return OperationResult<AttendanceRosterResult>.Failure("classAccessDenied");
        }

        var classroom = await dbContext.ClassRooms
            .Where(item => item.Id == classId)
            .Select(item => new { item.Id, item.Code })
            .SingleOrDefaultAsync(cancellationToken);
        if (classroom is null)
        {
            return OperationResult<AttendanceRosterResult>.Failure("classNotFound");
        }

        var enrolled = await (
            from enrollment in dbContext.StudentEnrollments.AsNoTracking()
            join student in dbContext.StudentProfiles.AsNoTracking() on enrollment.StudentProfileId equals student.Id
            join studentUser in dbContext.Users.AsNoTracking() on student.UserId equals studentUser.Id
            where enrollment.ClassId == classId
            orderby student.StudentCode
            select new
            {
                student.Id,
                student.StudentCode,
                studentUser.DisplayName
            }).ToListAsync(cancellationToken);

        var existing = await dbContext.AttendanceRecords
            .Where(record => record.ClassId == classId
                && record.AttendanceDate == attendanceDate
                && record.Session == session)
            .ToDictionaryAsync(record => record.StudentProfileId, cancellationToken);

        var entries = enrolled.Select(item =>
        {
            existing.TryGetValue(item.Id, out var found);
            return new AttendanceRosterEntry(
                item.Id,
                item.StudentCode,
                item.DisplayName,
                found?.Status ?? AttendanceStatus.Unmarked,
                found?.Note,
                found is null ? null : Convert.ToBase64String(found.RowVersion));
        }).ToList();

        return OperationResult<AttendanceRosterResult>.Success(new AttendanceRosterResult(
            classroom.Id, classroom.Code, attendanceDate, session.ToWire(), entries));
    }

    public async Task<OperationResult<AttendanceSaveResult>> SaveClassAttendanceAsync(
        SaveAttendanceCommand command,
        CancellationToken cancellationToken)
    {
        if (!await IsTeacherAssignedToClassAsync(command.ActorUserId, command.ClassId, cancellationToken))
        {
            return OperationResult<AttendanceSaveResult>.Failure("classAccessDenied");
        }
        if (command.Entries.Any(item => item.Status == AttendanceStatus.Unmarked))
        {
            return OperationResult<AttendanceSaveResult>.Failure("unmarkedEntryNotAllowed");
        }

        var enrolledIds = await (
            from enrollment in dbContext.StudentEnrollments.AsNoTracking()
            where enrollment.ClassId == command.ClassId
            select enrollment.StudentProfileId).ToListAsync(cancellationToken);

        var enrolledSet = new HashSet<Guid>(enrolledIds);
        foreach (var entry in command.Entries)
        {
            if (!enrolledSet.Contains(entry.StudentProfileId))
            {
                return OperationResult<AttendanceSaveResult>.Failure("studentNotEnrolled");
            }
        }

        var existing = await dbContext.AttendanceRecords
            .Where(record => record.ClassId == command.ClassId
                && record.AttendanceDate == command.AttendanceDate
                && record.Session == command.Session)
            .ToListAsync(cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();
        var saved = 0;
        foreach (var entry in command.Entries)
        {
            var record = existing.SingleOrDefault(item => item.StudentProfileId == entry.StudentProfileId);
            if (record is null)
            {
                record = new AttendanceRecord
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = entry.StudentProfileId,
                    ClassId = command.ClassId,
                    AttendanceDate = command.AttendanceDate,
                    Session = command.Session,
                    Status = entry.Status,
                    Note = entry.Note,
                    RecordedByUserId = command.ActorUserId,
                    RecordedAtUtc = nowUtc
                };
                dbContext.AttendanceRecords.Add(record);
            }
            else
            {
                if (string.IsNullOrEmpty(entry.RowVersion))
                    return OperationResult<AttendanceSaveResult>.Failure("concurrencyConflict");
                if (Convert.FromBase64String(entry.RowVersion) is { Length: > 0 } supplied
                    && !supplied.SequenceEqual(record.RowVersion))
                {
                    return OperationResult<AttendanceSaveResult>.Failure("concurrencyConflict");
                }
                record.Status = entry.Status;
                record.Note = entry.Note;
                record.RecordedByUserId = command.ActorUserId;
                record.RecordedAtUtc = nowUtc;
            }
            saved++;
        }

        dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
        {
            Id = Guid.NewGuid(),
            EventType = "attendanceSaved",
            ActorUserId = command.ActorUserId,
            SubjectUserId = command.ActorUserId,
            CorrelationId = command.CorrelationId,
            OccurredAtUtc = nowUtc
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return OperationResult<AttendanceSaveResult>.Failure("concurrencyConflict");
        }
        catch (DbUpdateException exception) when (IsUniqueConflict(exception))
        {
            return OperationResult<AttendanceSaveResult>.Failure("attendanceDuplicate");
        }

        var unmarked = command.Entries.Count(item => item.Status == AttendanceStatus.Unmarked);
        return OperationResult<AttendanceSaveResult>.Success(new AttendanceSaveResult(
            command.ClassId, command.AttendanceDate, command.Session.ToWire(), saved, unmarked));
    }

    public async Task<OperationResult<AttendanceHistoryPage>> GetStudentAttendanceHistoryAsync(
        Guid studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var boundedPage = page < 1 ? 1 : page;
        var boundedPageSize = pageSize switch
        {
            < 1 => 20,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };

        var query =
            from record in dbContext.AttendanceRecords.AsNoTracking()
            join classroom in dbContext.ClassRooms.AsNoTracking() on record.ClassId equals classroom.Id
            where record.StudentProfileId == studentProfileId
            orderby record.AttendanceDate descending, record.Session descending
            select new StudentAttendanceEntry(
                record.AttendanceDate,
                record.Session.ToWire(),
                classroom.DisplayName,
                record.Status,
                record.Note);

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)boundedPageSize);
        var items = await query
            .Skip((boundedPage - 1) * boundedPageSize)
            .Take(boundedPageSize)
            .ToListAsync(cancellationToken);

        return OperationResult<AttendanceHistoryPage>.Success(new AttendanceHistoryPage(
            items, boundedPage, boundedPageSize, totalCount, totalPages));
    }

    private Task<bool> IsTeacherAssignedToClassAsync(Guid actorUserId, Guid classId, CancellationToken cancellationToken) =>
        dbContext.TeacherClassSubjectAssignments.AsNoTracking()
            .Where(assignment => assignment.ClassId == classId && assignment.IsActive)
            .Join(dbContext.TeacherProfiles.AsNoTracking(),
                assignment => assignment.TeacherProfileId,
                profile => profile.Id,
                (assignment, profile) => new { assignment, profile })
            .AnyAsync(item => item.profile.UserId == actorUserId && item.profile.IsActive, cancellationToken);

    private static bool IsUniqueConflict(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
