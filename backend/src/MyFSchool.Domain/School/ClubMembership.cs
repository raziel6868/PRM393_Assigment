namespace MyFSchool.Domain.School;

public enum ClubMembershipStatus
{
    Pending = 0,
    Active = 1,
    Rejected = 2,
    Left = 3
}

public sealed class ClubMembership
{
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }

    public Guid StudentProfileId { get; set; }

    public ClubMembershipStatus Status { get; set; } = ClubMembershipStatus.Pending;

    public DateTimeOffset JoinedAtUtc { get; set; }

    public DateTimeOffset? LeftAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}

public static class ClubMembershipStatusExtensions
{
    public static string ToWire(this ClubMembershipStatus status) => status switch
    {
        ClubMembershipStatus.Pending => "pending",
        ClubMembershipStatus.Active => "active",
        ClubMembershipStatus.Rejected => "rejected",
        ClubMembershipStatus.Left => "left",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryFromWire(string? value, out ClubMembershipStatus status)
    {
        status = value switch
        {
            "pending" => ClubMembershipStatus.Pending,
            "active" => ClubMembershipStatus.Active,
            "rejected" => ClubMembershipStatus.Rejected,
            "left" => ClubMembershipStatus.Left,
            _ => default
        };
        return value is "pending" or "active" or "rejected" or "left";
    }
}
