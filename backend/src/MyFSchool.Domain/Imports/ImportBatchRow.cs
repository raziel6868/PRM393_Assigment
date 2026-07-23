namespace MyFSchool.Domain.Imports;

public sealed class ImportBatchRow
{
    public Guid Id { get; set; }

    public Guid BatchId { get; set; }

    public ImportBatch? Batch { get; set; }

    public string SheetName { get; set; } = string.Empty;

    public int SheetRowNumber { get; set; }

    public string? ReferenceCode { get; set; }

    public string ErrorCode { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public string? ColumnName { get; set; }
}
