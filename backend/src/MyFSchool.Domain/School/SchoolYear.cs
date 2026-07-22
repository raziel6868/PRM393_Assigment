namespace MyFSchool.Domain.School;

public sealed class SchoolYear
{
    public Guid Id { get; set; }

    public required string Code { get; set; }

    public required string DisplayName { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
