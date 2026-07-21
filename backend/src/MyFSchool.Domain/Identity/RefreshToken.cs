namespace MyFSchool.Domain.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid FamilyId { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public DateTimeOffset? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? RevocationReason { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public bool IsActive(DateTimeOffset nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
}
