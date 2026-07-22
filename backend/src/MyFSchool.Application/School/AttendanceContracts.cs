using MyFSchool.Domain.School;
using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record AttendanceRosterEntry(
    Guid StudentProfileId,
    string StudentCode,
    string StudentDisplayName,
    AttendanceStatus Status,
    string? Note,
    string? RowVersion);

public sealed record AttendanceRosterResult(
    Guid ClassId,
    string ClassCode,
    DateOnly AttendanceDate,
    string Session,
    IReadOnlyList<AttendanceRosterEntry> Entries);

public sealed record AttendanceEntryUpdate(
    Guid StudentProfileId,
    AttendanceStatus Status,
    string? Note,
    string? RowVersion);

public sealed record SaveAttendanceCommand(
    Guid ClassId,
    DateOnly AttendanceDate,
    SchoolSession Session,
    IReadOnlyList<AttendanceEntryUpdate> Entries,
    Guid ActorUserId,
    string CorrelationId);

public sealed record AttendanceSaveResult(
    Guid ClassId,
    DateOnly AttendanceDate,
    string Session,
    int SavedCount,
    int UnmarkedCount);

public sealed record StudentAttendanceEntry(
    DateOnly AttendanceDate,
    string Session,
    string ClassDisplayName,
    AttendanceStatus Status,
    string? Note);

public sealed record AttendanceHistoryPage(
    IReadOnlyList<StudentAttendanceEntry> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public interface IAttendanceAdministrationService
{
    Task<OperationResult<AttendanceRosterResult>> GetClassRosterAsync(
        Guid actorUserId,
        Guid classId,
        DateOnly attendanceDate,
        SchoolSession session,
        CancellationToken cancellationToken);

    Task<OperationResult<AttendanceSaveResult>> SaveClassAttendanceAsync(
        SaveAttendanceCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<AttendanceHistoryPage>> GetStudentAttendanceHistoryAsync(
        Guid studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
