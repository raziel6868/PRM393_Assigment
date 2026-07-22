using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record TimetableEntryResult(
    Guid Id,
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SubjectId,
    string SubjectCode,
    string SubjectDisplayName,
    Guid TeacherProfileId,
    string TeacherDisplayName,
    Guid SchoolYearId,
    int DayOfWeek,
    string StartTime,
    string EndTime,
    string? Room,
    string SessionState);

public sealed record EventResult(
    Guid Id,
    string Title,
    string? Description,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    string? Location,
    string? OrganizerContact,
    string Audience,
    string Status);

public sealed record EventPage(
    IReadOnlyList<EventResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CreateEventCommand(
    string Title,
    string? Description,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    string? Location,
    string? OrganizerContact,
    string Audience,
    Guid ActorUserId,
    string CorrelationId);

public sealed record EventPageResult(
    IReadOnlyList<EventResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public interface ITimetableQueryService
{
    Task<IReadOnlyList<TimetableEntryResult>> GetWeekTimetableAsync(
        Guid userId,
        string role,
        Guid? studentProfileId,
        DateOnly weekStart,
        CancellationToken cancellationToken);
}

public interface IEventQueryService
{
    Task<OperationResult<EventPageResult>> GetUpcomingEventsAsync(
        Guid? userId,
        string? role,
        Guid? studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OperationResult<EventResult>> GetEventDetailAsync(
        Guid eventId,
        CancellationToken cancellationToken);
}
