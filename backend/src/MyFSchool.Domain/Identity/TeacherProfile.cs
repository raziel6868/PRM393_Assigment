namespace MyFSchool.Domain.Identity;

public sealed class TeacherProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string EmployeeCode { get; set; }

    public bool IsActive { get; set; } = true;
}
