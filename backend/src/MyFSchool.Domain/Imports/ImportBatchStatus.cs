namespace MyFSchool.Domain.Imports;

public enum ImportBatchStatus
{
    Staged = 0,
    Validated = 1,
    Committed = 2,
    Failed = 3,
}
