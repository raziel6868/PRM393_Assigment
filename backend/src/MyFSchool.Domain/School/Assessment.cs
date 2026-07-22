namespace MyFSchool.Domain.School;

public sealed class Assessment
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public string AssessmentType { get; set; } = "regular";

    public Guid SchoolYearId { get; set; }

    public int Semester { get; set; } = 1;

    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public decimal MinScore { get; set; } = 0m;

    public decimal MaxScore { get; set; } = 10m;

    public int Weight { get; set; } = 1;

    public DateOnly? DueDate { get; set; }

    public bool IsPublished { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
