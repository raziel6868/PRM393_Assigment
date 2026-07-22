namespace MyFSchool.Domain.School;

public sealed class ClassRoom
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public int GradeLevel { get; set; }

    public Guid SchoolYearId { get; set; }

    public Guid? HomeroomTeacherProfileId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
