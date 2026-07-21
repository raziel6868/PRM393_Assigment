namespace MyFSchool.Domain.Identity;

public sealed class ParentProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string ParentCode { get; set; }

    public bool IsActive { get; set; } = true;
}
