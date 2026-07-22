namespace MyFSchool.Domain.School;

public sealed class StudentEnrollment
{
    public Guid Id { get; set; }

    public Guid StudentProfileId { get; set; }

    public Guid ClassId { get; set; }

    public Guid SchoolYearId { get; set; }

    public DateOnly EnrolledOn { get; set; }

    public DateOnly? LeftOn { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
