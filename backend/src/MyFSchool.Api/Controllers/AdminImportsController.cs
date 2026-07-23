using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFSchool.Application.Identity;
using MyFSchool.Application.Imports;
using MyFSchool.Domain.School;

namespace MyFSchool.Api.Controllers;

[ApiController]
[Authorize(Policy = SchoolPolicies.Administrator)]
[Route("api/v1/admin/imports")]
public sealed class AdminImportsController(IExcelImportService excelImportService) : ControllerBase
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("template")]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var descriptor = await excelImportService.GetTemplateDescriptorAsync(cancellationToken);
        var bytes = await excelImportService.RenderTemplateAsync(cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(bytes, descriptor.ContentType, descriptor.FileName);
    }

    [HttpGet("template/info")]
    public async Task<IActionResult> GetTemplateInfo(CancellationToken cancellationToken)
    {
        var descriptor = await excelImportService.GetTemplateDescriptorAsync(cancellationToken);
        return Ok(new
        {
            templateVersion = "1.0.0",
            contentType = descriptor.ContentType,
            fileName = descriptor.FileName,
            sheets = descriptor.SheetNames,
            sheetHeaders = descriptor.SheetHeaders,
            instructionsSheet = "Instructions",
        });
    }

    [HttpPost("")]
    [RequestSizeLimit(10L * 1024 * 1024)]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();

        var form = await Request.ReadFormAsync(cancellationToken);
        var file = form.Files.FirstOrDefault();
        if (file == null || file.Length == 0)
        {
            return ProblemResponse(400, "uploadEmpty", "Không có tệp", "Vui lòng chọn tệp Excel (.xlsx) để tải lên.");
        }

        if (!string.Equals(file.ContentType, ExcelContentType, StringComparison.OrdinalIgnoreCase))
        {
            return ProblemResponse(400, "uploadInvalidContentType",
                "Content-Type không hợp lệ",
                "Tệp tải lên phải có Content-Type là application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.");
        }

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        var fileBytes = memory.ToArray();

        var result = await excelImportService.UploadAsync(
            new UploadImportBatchCommand(
                FileName: file.FileName,
                Content: fileBytes,
                ActorUserId: actorUserId,
                CorrelationId: HttpContext.TraceIdentifier),
            cancellationToken);

        if (result.IsSuccess)
        {
            Response.Headers.CacheControl = "no-store";
            return Created($"/api/v1/admin/imports/{result.Value!.BatchId}", result.Value);
        }
        return MapFailure(result.ErrorCode!);
    }

    [HttpPost("{batchId:guid}/validate")]
    public async Task<IActionResult> Validate(Guid batchId, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        var result = await excelImportService.ValidateAsync(
            batchId, actorUserId, HttpContext.TraceIdentifier, cancellationToken);
        if (result.IsSuccess) return Ok(result.Value!);
        return MapFailure(result.ErrorCode!);
    }

    [HttpPost("{batchId:guid}/commit")]
    public async Task<IActionResult> Commit(Guid batchId, CancellationToken cancellationToken)
    {
        if (!TryGetActorUserId(out var actorUserId)) return Unauthorized();
        var result = await excelImportService.CommitAsync(
            new CommitImportBatchCommand(batchId, actorUserId, HttpContext.TraceIdentifier),
            cancellationToken);
        if (result.IsSuccess) return Ok(result.Value!);
        return MapFailure(result.ErrorCode!);
    }

    [HttpGet("{batchId:guid}")]
    public async Task<IActionResult> Get(Guid batchId, CancellationToken cancellationToken)
    {
        var result = await excelImportService.GetAsync(batchId, cancellationToken);
        if (result.IsSuccess) return Ok(result.Value!);
        return MapFailure(result.ErrorCode!);
    }

    [HttpGet("{batchId:guid}/result")]
    public async Task<IActionResult> GetResult(Guid batchId, CancellationToken cancellationToken)
    {
        var summary = await excelImportService.GetAsync(batchId, cancellationToken);
        if (summary.IsSuccess)
        {
            return Ok(new
            {
                batchId = summary.Value!.BatchId,
                fileName = summary.Value.FileName,
                fileSizeBytes = summary.Value.FileSizeBytes,
                status = summary.Value.Status,
                hasBlockingErrors = summary.Value.HasBlockingErrors,
                rowCount = summary.Value.RowCount,
                createdUserCount = summary.Value.CreatedUserCount,
                updatedUserCount = summary.Value.UpdatedUserCount,
                createdProfileCount = summary.Value.CreatedProfileCount,
                createdLinkCount = summary.Value.CreatedLinkCount,
                createdAssignmentCount = summary.Value.CreatedAssignmentCount,
                createdEnrollmentCount = summary.Value.CreatedEnrollmentCount,
                failureReason = summary.Value.FailureReason,
                createdAtUtc = summary.Value.CreatedAtUtc,
                validatedAtUtc = summary.Value.ValidatedAtUtc,
                committedAtUtc = summary.Value.CommittedAtUtc,
                templateVersion = "1.0.0",
            });
        }
        return MapFailure(summary.ErrorCode!);
    }

    private bool TryGetActorUserId(out Guid actorUserId) =>
        Guid.TryParse(User.FindFirst("sub")?.Value, out actorUserId);

    private IActionResult MapFailure(string errorCode) => errorCode switch
    {
        "importBatchNotFound" => ProblemResponse(404, "importBatchNotFound",
            "Không tìm thấy lô nhập", "Lô nhập liệu không tồn tại."),
        "importBatchNotValidated" => ProblemResponse(409, "importBatchNotValidated",
            "Chưa xác nhận hợp lệ", "Vui lòng chạy bước xác nhận trước khi lưu."),
        "importBatchAlreadyCommitted" => ProblemResponse(409, "importBatchAlreadyCommitted",
            "Lô nhập đã được lưu", "Lô nhập liệu này đã được lưu vào hệ thống trước đó."),
        "importCommitFailed" => ProblemResponse(500, "importCommitFailed",
            "Không thể lưu lô nhập", "Hệ thống gặp sự cố khi lưu lô nhập liệu."),
        "importStageFailed" => ProblemResponse(500, "importStageFailed",
            "Không thể lưu tệp tạm", "Hệ thống gặp sự cố khi tạo lô nhập liệu."),
        "uploadEmpty" => ProblemResponse(400, "uploadEmpty",
            "Tệp rỗng", "Vui lòng chọn tệp Excel không rỗng."),
        "uploadTooLarge" => ProblemResponse(413, "uploadTooLarge",
            "Tệp quá lớn", "Vui lòng tải lên tệp .xlsx nhỏ hơn 10 MB."),
        "uploadInvalidExtension" => ProblemResponse(400, "uploadInvalidExtension",
            "Định dạng tệp không hợp lệ", "Vui lòng chọn tệp Excel (.xlsx)."),
        "uploadInvalidContentType" => ProblemResponse(400, "uploadInvalidContentType",
            "Content-Type không hợp lệ", "Tệp phải có Content-Type application/vnd.openxmlformats-officedocument.spreadsheetml.sheet."),
        "workbookStructureInvalid" => ProblemResponse(400, "workbookStructureInvalid",
            "Không đọc được tệp", "Tệp Excel không đúng cấu trúc chuẩn (.xlsx)."),
        "workbookHeaderMissing" => ProblemResponse(400, "workbookHeaderMissing",
            "Thiếu sheet/header chuẩn", "Tệp Excel thiếu sheet hoặc cột đúng chuẩn mẫu."),
        "workbookContainsFormula" => ProblemResponse(400, "workbookContainsFormula",
            "Ô chứa công thức", "Tệp Excel chứa ô có công thức; vui lòng thay bằng giá trị tĩnh."),
        _ => ProblemResponse(400, errorCode, "Không thể xử lý yêu cầu", "Vui lòng kiểm tra thông tin và thử lại."),
    };

    private ObjectResult ProblemResponse(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
