namespace MyFSchool.Domain.Identity;

public sealed class SecurityAuditEvent
{
    public Guid Id { get; set; }

    public required string EventType { get; set; }

    public Guid? SubjectUserId { get; set; }

    public Guid? ActorUserId { get; set; }

    public required string CorrelationId { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }
}
