namespace MyFSchool.Domain.School;

public sealed class TeacherClassSubjectAssignment
{
    public Guid Id { get; set; }

    public Guid TeacherProfileId { get; set; }

    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid SchoolYearId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
