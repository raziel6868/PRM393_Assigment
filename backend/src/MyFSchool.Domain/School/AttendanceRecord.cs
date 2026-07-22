namespace MyFSchool.Domain.School;

public sealed class AttendanceRecord
{
    public Guid Id { get; set; }

    public Guid StudentProfileId { get; set; }

    public Guid ClassId { get; set; }

    public DateOnly AttendanceDate { get; set; }

    public SchoolSession Session { get; set; }

    public AttendanceStatus Status { get; set; }

    public string? Note { get; set; }

    public Guid RecordedByUserId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
