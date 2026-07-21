namespace MyFSchool.Domain.Identity;

public sealed class StudentProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string StudentCode { get; set; }

    public bool IsActive { get; set; } = true;
}
