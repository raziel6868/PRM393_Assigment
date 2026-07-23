using MyFSchool.Application.Identity;

namespace MyFSchool.Application.Imports;

public interface IExcelImportService
{
    Task<ImportTemplateDescriptor> GetTemplateDescriptorAsync(CancellationToken cancellationToken);

    Task<byte[]> RenderTemplateAsync(CancellationToken cancellationToken);

    Task<OperationResult<ImportBatchSummary>> UploadAsync(
        UploadImportBatchCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ImportValidationReport>> ValidateAsync(
        Guid batchId,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken);

    Task<OperationResult<ImportBatchSummary>> CommitAsync(
        CommitImportBatchCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<ImportBatchSummary>> GetAsync(
        Guid batchId,
        CancellationToken cancellationToken);
}
