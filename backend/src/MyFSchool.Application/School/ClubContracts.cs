using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record ClubResult(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Category,
    int? MaxMembers,
    int CurrentMemberCount,
    bool IsActive,
    string RowVersion);

public sealed record ClubDetailResult(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Category,
    int? MaxMembers,
    int CurrentMemberCount,
    bool IsActive,
    string RowVersion,
    string MembershipStatus,
    DateTimeOffset? JoinedAtUtc);

public sealed record ClubPage(
    IReadOnlyList<ClubResult> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record JoinClubCommand(
    Guid ClubId,
    Guid ActorUserId,
    string CorrelationId);

public sealed record LeaveClubCommand(
    Guid ClubId,
    Guid ActorUserId,
    string CorrelationId);

public sealed record CreateClubCommand(
    string Code,
    string DisplayName,
    string? Description,
    string Category,
    int? MaxMembers,
    Guid ActorUserId,
    string CorrelationId);

public sealed record UpdateClubCommand(
    Guid ClubId,
    string DisplayName,
    string? Description,
    string Category,
    int? MaxMembers,
    bool IsActive,
    byte[] RowVersion,
    Guid ActorUserId,
    string CorrelationId);

public interface IClubAdministrationService
{
    Task<OperationResult<ClubResult>> CreateAsync(
        CreateClubCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ClubResult>> UpdateAsync(
        UpdateClubCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ClubPage>> ListPublicAsync(
        string? search,
        string? category,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OperationResult<ClubDetailResult>> GetDetailAsync(
        Guid clubId,
        Guid? actorUserId,
        CancellationToken cancellationToken);

    Task<OperationResult<ClubDetailResult>> JoinAsync(
        JoinClubCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ClubDetailResult>> LeaveAsync(
        LeaveClubCommand command,
        CancellationToken cancellationToken);
}
