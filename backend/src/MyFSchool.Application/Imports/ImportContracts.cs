using MyFSchool.Domain.Identity;

namespace MyFSchool.Application.Imports;

public enum ImportErrorCode
{
    None,
    WorkbookStructureInvalid,
    MissingRequiredField,
    InvalidEmailFormat,
    InvalidDateFormat,
    DuplicateCodeInBatch,
    DuplicateCodeAgainstDatabase,
    ClassCodeNotFound,
    SubjectCodeNotFound,
    SchoolYearCodeNotFound,
    ParentOrStudentCodeMissing,
    MultipleEnrollmentsForStudent,
    MultipleAssignmentsForTeacher,
}

public sealed record ImportTemplateDescriptor(
    string FileName,
    string ContentType,
    int SheetCount,
    IReadOnlyList<string> SheetNames,
    IReadOnlyDictionary<string, IReadOnlyList<string>> SheetHeaders);

public sealed record ImportRowError(
    string SheetName,
    int SheetRowNumber,
    string ErrorCode,
    string ErrorMessage,
    string? ColumnName,
    string? ReferenceCode);

public sealed record ImportValidationReport(
    Guid BatchId,
    string Status,
    bool HasBlockingErrors,
    int RowCount,
    int BlockingErrorCount,
    int WarningCount,
    IReadOnlyList<ImportRowError> Errors,
    IReadOnlyList<string> Warnings);

public sealed record ImportBatchSummary(
    Guid BatchId,
    string FileName,
    long FileSizeBytes,
    string Status,
    bool HasBlockingErrors,
    int RowCount,
    int CreatedUserCount,
    int UpdatedUserCount,
    int CreatedProfileCount,
    int CreatedLinkCount,
    int CreatedAssignmentCount,
    int CreatedEnrollmentCount,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ValidatedAtUtc,
    DateTimeOffset? CommittedAtUtc);

public sealed record UploadImportBatchCommand(
    string FileName,
    byte[] Content,
    Guid ActorUserId,
    string CorrelationId);

public sealed record CommitImportBatchCommand(
    Guid BatchId,
    Guid ActorUserId,
    string CorrelationId);
