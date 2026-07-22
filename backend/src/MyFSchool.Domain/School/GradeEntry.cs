namespace MyFSchool.Domain.School;

public sealed class GradeEntry
{
    public Guid Id { get; set; }

    public Guid AssessmentId { get; set; }

    public Guid StudentProfileId { get; set; }

    public decimal? Score { get; set; }

    public string? TeacherComment { get; set; }

    public Guid RecordedByUserId { get; set; }

    public DateTimeOffset RecordedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
