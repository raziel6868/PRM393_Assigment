namespace MyFSchool.Domain.Identity;

public sealed class PasswordHelpRequest
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public PasswordHelpStatus Status { get; set; }

    public DateTimeOffset RequestedAtUtc { get; set; }

    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public Guid? ResolvedByUserId { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
