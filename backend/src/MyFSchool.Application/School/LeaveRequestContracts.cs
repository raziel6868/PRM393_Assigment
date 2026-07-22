using MyFSchool.Domain.School;
using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record SubmitLeaveRequestCommand(
    Guid StudentProfileId,
    DateOnly StartDate,
    DateOnly EndDate,
    SchoolSession StartSession,
    SchoolSession EndSession,
    LeaveReasonCategory ReasonCategory,
    string Reason,
    Guid ActorUserId,
    string CorrelationId);

public sealed record LeaveRequestResult(
    Guid Id,
    Guid StudentProfileId,
    Guid RequesterUserId,
    DateOnly StartDate,
    DateOnly EndDate,
    string StartSession,
    string EndSession,
    string ReasonCategory,
    string Reason,
    string? DecisionNote,
    Guid? ReviewerUserId,
    DateTimeOffset? ReviewedAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DecidedAtUtc,
    string RowVersion);

public sealed record CancelLeaveRequestCommand(
    Guid LeaveRequestId,
    byte[] RowVersion,
    Guid ActorUserId,
    string CorrelationId);

public sealed record DecideLeaveRequestCommand(
    Guid LeaveRequestId,
    bool Approve,
    string? DecisionNote,
    byte[] RowVersion,
    Guid ActorUserId,
    string CorrelationId);

public sealed record LeaveRequestPage(
    IReadOnlyList<LeaveRequestResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public interface ILeaveRequestAdministrationService
{
    Task<OperationResult<LeaveRequestResult>> SubmitAsync(
        SubmitLeaveRequestCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<LeaveRequestResult>> CancelAsync(
        CancelLeaveRequestCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<LeaveRequestPage>> ListMineAsync(
        Guid actorUserId,
        Guid? studentProfileId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OperationResult<LeaveRequestResult>> GetForParentAsync(
        Guid actorUserId,
        Guid leaveRequestId,
        CancellationToken cancellationToken);

    Task<OperationResult<LeaveRequestPage>> ListTeacherQueueAsync(
        Guid actorUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OperationResult<LeaveRequestResult>> DecideAsync(
        DecideLeaveRequestCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<LeaveRequestResult>> GetForTeacherAsync(
        Guid actorUserId,
        Guid leaveRequestId,
        CancellationToken cancellationToken);
}