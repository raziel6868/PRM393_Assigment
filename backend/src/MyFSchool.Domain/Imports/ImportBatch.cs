using MyFSchool.Domain.Identity;

namespace MyFSchool.Domain.Imports;

public sealed class ImportBatch
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string OriginalFileSha256 { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public ImportBatchStatus Status { get; set; } = ImportBatchStatus.Staged;

    public bool HasBlockingErrors { get; set; }

    public int RowCount { get; set; }

    public int CreatedUserCount { get; set; }

    public int UpdatedUserCount { get; set; }

    public int CreatedProfileCount { get; set; }

    public int CreatedLinkCount { get; set; }

    public int CreatedAssignmentCount { get; set; }

    public int CreatedEnrollmentCount { get; set; }

    public string? FailureReason { get; set; }

    public Guid CreatedByUserId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset? ValidatedAtUtc { get; set; }

    public DateTimeOffset? CommittedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<ImportBatchRow> Rows { get; set; } = new List<ImportBatchRow>();
}
