namespace MyFSchool.Domain.School;

public sealed class LeaveRequest
{
    public Guid Id { get; set; }

    public Guid StudentProfileId { get; set; }

    public Guid RequesterUserId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public SchoolSession StartSession { get; set; }

    public SchoolSession EndSession { get; set; }

    public LeaveReasonCategory ReasonCategory { get; set; }

    public required string Reason { get; set; }

    public string? DecisionNote { get; set; }

    public Guid? ReviewerUserId { get; set; }

    public DateTimeOffset? ReviewedAtUtc { get; set; }

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? DecidedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}