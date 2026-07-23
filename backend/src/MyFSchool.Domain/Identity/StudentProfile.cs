namespace MyFSchool.Domain.Identity;

public sealed class StudentProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string StudentCode { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Student date of birth captured by the school-provided roster.
    /// Persisted as a date-only value and never used as the StudentEnrollment.EnrolledOn date.
    /// </summary>
    public DateOnly? DateOfBirth { get; set; }
}
