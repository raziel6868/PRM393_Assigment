namespace MyFSchool.Domain.School;

public sealed class TimetableEntry
{
    public Guid Id { get; set; }

    public Guid ClassId { get; set; }

    public Guid SubjectId { get; set; }

    public Guid TeacherProfileId { get; set; }

    public Guid SchoolYearId { get; set; }

    public int DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string? Room { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class SchoolEvent
{
    public Guid Id { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset StartAtUtc { get; set; }

    public DateTimeOffset EndAtUtc { get; set; }

    public string? Location { get; set; }

    public string? OrganizerContact { get; set; }

    public string Audience { get; set; } = "all";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
