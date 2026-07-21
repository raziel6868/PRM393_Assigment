namespace MyFSchool.Domain.Identity;

public sealed class ParentStudentLink
{
    public Guid Id { get; set; }

    public Guid ParentProfileId { get; set; }

    public Guid StudentProfileId { get; set; }

    public GuardianRelationship Relationship { get; set; }

    public bool IsPrimaryContact { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }
}
