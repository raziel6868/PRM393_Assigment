using System.Globalization;
using System.Security.Cryptography;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyFSchool.Application.Identity;
using MyFSchool.Application.Imports;
using MyFSchool.Domain.Identity;
using MyFSchool.Domain.Imports;
using MyFSchool.Domain.School;
using MyFSchool.Infrastructure.Identity;
using MyFSchool.Infrastructure.Persistence;

namespace MyFSchool.Infrastructure.Imports;

public sealed class ExcelImportService(
    MyFSchoolDbContext dbContext,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    TimeProvider timeProvider,
    ILogger<ExcelImportService> logger) : IExcelImportService
{
    private const long MaxFileSizeBytes = 10L * 1024 * 1024;
    public const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string TemplateVersion = "1.0.0";
    public const string InstructionsSheetName = "Instructions";
    private static readonly string[] AllowedRelationships = { "father", "mother", "guardian", "other" };

    public Task<ImportTemplateDescriptor> GetTemplateDescriptorAsync(CancellationToken cancellationToken) =>
        Task.FromResult(new ImportTemplateDescriptor(
            FileName: "MyFSchool_Import_Template.xlsx",
            ContentType: ExcelContentType,
            SheetCount: ExcelTemplateBuilder.Sheets.Count + 1,
            SheetNames: new[] { InstructionsSheetName }
                .Concat(ExcelTemplateBuilder.Sheets)
                .ToArray(),
            SheetHeaders: ExcelTemplateBuilder.Headers));

    public Task<byte[]> RenderTemplateAsync(CancellationToken cancellationToken) =>
        Task.FromResult(ExcelTemplateBuilder.Render(TemplateVersion));

    public async Task<OperationResult<ImportBatchSummary>> UploadAsync(
        UploadImportBatchCommand command,
        CancellationToken cancellationToken)
    {
        if (command.Content is null || command.Content.Length == 0)
        {
            return OperationResult<ImportBatchSummary>.Failure("uploadEmpty");
        }

        if (command.Content.Length > MaxFileSizeBytes)
        {
            return OperationResult<ImportBatchSummary>.Failure("uploadTooLarge");
        }

        if (!command.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<ImportBatchSummary>.Failure("uploadInvalidExtension");
        }

        // Structural parse and formula detection are kept strictly separate:
        //   workbookStructureInvalid       -> the bytes are not a valid XLSX at all
        //   workbookHeaderMissing          -> a sheet/header is missing or reordered
        //   workbookContainsFormula        -> a cell contains a formula (always rejected)
        var parseOutcome = WorkbookStructure.Parse(command.Content, logger, command.FileName);
        if (parseOutcome.Kind == WorkbookStructure.ParseKind.Unreadable)
        {
            logger.LogWarning("Excel upload rejected as unreadable for {FileName}.", command.FileName);
            return OperationResult<ImportBatchSummary>.Failure("workbookStructureInvalid");
        }

        using var workbook = parseOutcome.Workbook!;
        var formulaOutcome = WorkbookStructure.HasFormula(workbook);
        if (formulaOutcome.HasFormula)
        {
            logger.LogWarning(
                "Excel upload rejected — formula cell detected on sheet {Sheet} column {Column} row {Row}.",
                formulaOutcome.SheetName, formulaOutcome.ColumnName, formulaOutcome.RowNumber);
            return OperationResult<ImportBatchSummary>.Failure("workbookContainsFormula");
        }

        var sha256 = Convert.ToHexString(SHA256.HashData(command.Content)).ToLowerInvariant();

        var batch = new ImportBatch
        {
            Id = Guid.NewGuid(),
            FileName = Path.GetFileName(command.FileName),
            OriginalFileSha256 = sha256,
            FileSizeBytes = command.Content.Length,
            Status = ImportBatchStatus.Staged,
            CreatedByUserId = command.ActorUserId,
            CreatedAtUtc = timeProvider.GetUtcNow(),
            RowVersion = [],
        };

        try
        {
            var stagingRows = ParseStagingRows(workbook, batch.Id);
            batch.RowCount = stagingRows.Count;
            dbContext.ImportBatches.Add(batch);
            dbContext.ImportBatchRows.AddRange(stagingRows);
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Imported batch {BatchId} staged with {RowCount} rows by {ActorUserId}.",
                batch.Id, stagingRows.Count, command.ActorUserId);
            return OperationResult<ImportBatchSummary>.Success(ToSummary(batch));
        }
        catch (WorkbookHeaderMissingException headerEx)
        {
            logger.LogWarning(headerEx, "Excel upload rejected — header missing on sheet {Sheet}.", headerEx.SheetName);
            return OperationResult<ImportBatchSummary>.Failure("workbookHeaderMissing");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Excel import staging persistence failed for {FileName}.", command.FileName);
            return OperationResult<ImportBatchSummary>.Failure("importStageFailed");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Excel import staging failed for {FileName}.", command.FileName);
            return OperationResult<ImportBatchSummary>.Failure("importStageFailed");
        }
    }

    public async Task<OperationResult<ImportValidationReport>> ValidateAsync(
        Guid batchId,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var batch = await dbContext.ImportBatches
            .Include(item => item.Rows)
            .FirstOrDefaultAsync(item => item.Id == batchId, cancellationToken);

        if (batch is null)
        {
            return OperationResult<ImportValidationReport>.Failure("importBatchNotFound");
        }

        // Load DB-side reference data once so DB-aware checks (codes, emails, school year
        // alignment) run inside a single bounded snapshot.
        var classList = await dbContext.ClassRooms
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var classLookup = classList.ToDictionary(item => item.Code, StringComparer.Ordinal);
        var classSchoolYearIds = classList.ToDictionary(item => item.Code, item => item.SchoolYearId, StringComparer.Ordinal);
        var schoolYears = await dbContext.SchoolYears
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var schoolYearCodeLookup = new Dictionary<string, SchoolYear>(StringComparer.Ordinal);
        var schoolYearIdToCode = new Dictionary<Guid, string>();
        foreach (var sy in schoolYears)
        {
            schoolYearCodeLookup[sy.Code] = sy;
            schoolYearIdToCode[sy.Id] = sy.Code;
        }
        var schoolYearSet = new HashSet<string>(schoolYearCodeLookup.Keys, StringComparer.Ordinal);
        var subjectCodes = await dbContext.Subjects
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);
        var subjectSet = new HashSet<string>(subjectCodes, StringComparer.Ordinal);
        var studentCodesInDb = await dbContext.StudentProfiles
            .Select(item => item.StudentCode)
            .ToListAsync(cancellationToken);
        var parentCodesInDb = await dbContext.ParentProfiles
            .Select(item => item.ParentCode)
            .ToListAsync(cancellationToken);
        var teacherCodesInDb = await dbContext.TeacherProfiles
            .Select(item => item.EmployeeCode)
            .ToListAsync(cancellationToken);
        var emailsInDb = await dbContext.Users
            .Where(user => user.Email != null)
            .Select(user => user.NormalizedEmail!)
            .ToListAsync(cancellationToken);
        var normalizedEmailSet = new HashSet<string>(emailsInDb, StringComparer.OrdinalIgnoreCase);

        var crossSheetErrors = new List<ImportRowError>();
        var stagedSnapshot = batch.Rows
            .Where(row => row.ErrorCode == "staging")
            .ToList();
        ValidateStagedRows(stagedSnapshot, crossSheetErrors,
            classSchoolYearIds, schoolYearCodeLookup, schoolYearIdToCode,
            subjectSet, schoolYearSet,
            new HashSet<string>(studentCodesInDb, StringComparer.Ordinal),
            new HashSet<string>(parentCodesInDb, StringComparer.Ordinal),
            new HashSet<string>(teacherCodesInDb, StringComparer.Ordinal),
            normalizedEmailSet);

        // Persist newly discovered validation errors and clear prior validation errors.
        var previousValidationErrors = batch.Rows
            .Where(row => row.ErrorCode != "staging")
            .ToList();
        if (previousValidationErrors.Count > 0)
        {
            dbContext.ImportBatchRows.RemoveRange(previousValidationErrors);
        }
        if (crossSheetErrors.Count > 0)
        {
            foreach (var error in crossSheetErrors)
            {
                dbContext.ImportBatchRows.Add(new ImportBatchRow
                {
                    Id = Guid.NewGuid(),
                    BatchId = batch.Id,
                    SheetName = error.SheetName,
                    SheetRowNumber = error.SheetRowNumber,
                    ReferenceCode = error.ReferenceCode,
                    ErrorCode = error.ErrorCode,
                    ErrorMessage = error.ErrorMessage,
                    ColumnName = error.ColumnName,
                });
            }
        }

        batch.Status = ImportBatchStatus.Validated;
        batch.HasBlockingErrors = crossSheetErrors.Count > 0;
        batch.ValidatedAtUtc = timeProvider.GetUtcNow();
        await dbContext.SaveChangesAsync(cancellationToken);

        var report = new ImportValidationReport(
            BatchId: batch.Id,
            Status: batch.Status.ToString(),
            HasBlockingErrors: batch.HasBlockingErrors,
            RowCount: batch.RowCount,
            BlockingErrorCount: crossSheetErrors.Count,
            WarningCount: 0,
            Errors: crossSheetErrors,
            Warnings: Array.Empty<string>());

        logger.LogInformation(
            "Imported batch {BatchId} validated by {ActorUserId}: blocking={Blocking}.",
            batch.Id, actorUserId, batch.HasBlockingErrors);
        return OperationResult<ImportValidationReport>.Success(report);
    }

    public async Task<OperationResult<ImportBatchSummary>> CommitAsync(
        CommitImportBatchCommand command,
        CancellationToken cancellationToken)
    {
        var batch = await dbContext.ImportBatches
            .Include(item => item.Rows)
            .FirstOrDefaultAsync(item => item.Id == command.BatchId, cancellationToken);
        if (batch is null)
        {
            return OperationResult<ImportBatchSummary>.Failure("importBatchNotFound");
        }
        if (batch.Status == ImportBatchStatus.Committed)
        {
            return OperationResult<ImportBatchSummary>.Failure("importBatchAlreadyCommitted");
        }
        if (batch.Status != ImportBatchStatus.Validated || batch.HasBlockingErrors)
        {
            return OperationResult<ImportBatchSummary>.Failure("importBatchNotValidated");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var studentRows = batch.Rows
                .Where(row => row.SheetName == ExcelTemplateBuilder.SheetStudents)
                .Select(row => new StagedStudent(
                    row.SheetRowNumber,
                    Code: GetCell(row, "studentCode"),
                    FullName: GetCell(row, "fullName"),
                    DateOfBirth: GetCell(row, "dateOfBirth"),
                    ClassCode: GetCell(row, "classCode"),
                    UserName: GetCell(row, "userName"),
                    Email: GetCell(row, "email")))
                .ToList();
            var parentRows = batch.Rows
                .Where(row => row.SheetName == ExcelTemplateBuilder.SheetParents)
                .Select(row => new StagedParent(
                    row.SheetRowNumber,
                    Code: GetCell(row, "parentCode"),
                    FullName: GetCell(row, "fullName"),
                    Email: GetCell(row, "email"),
                    Phone: GetCell(row, "phone")))
                .ToList();
            var teacherRows = batch.Rows
                .Where(row => row.SheetName == ExcelTemplateBuilder.SheetTeachers)
                .Select(row => new StagedTeacher(
                    row.SheetRowNumber,
                    Code: GetCell(row, "employeeCode"),
                    FullName: GetCell(row, "fullName"),
                    Email: GetCell(row, "email"),
                    Phone: GetCell(row, "phone")))
                .ToList();
            var linkRows = batch.Rows
                .Where(row => row.SheetName == ExcelTemplateBuilder.SheetParentStudentLinks)
                .Select(row => new StagedParentStudentLink(
                    row.SheetRowNumber,
                    ParentCode: GetCell(row, "parentCode"),
                    StudentCode: GetCell(row, "studentCode"),
                    Relationship: GetCell(row, "relationship"),
                    IsPrimaryContact: GetCell(row, "isPrimaryContact")))
                .ToList();
            var assignmentRows = batch.Rows
                .Where(row => row.SheetName == ExcelTemplateBuilder.SheetTeacherAssignments)
                .Select(row => new StagedTeacherAssignment(
                    row.SheetRowNumber,
                    EmployeeCode: GetCell(row, "employeeCode"),
                    ClassCode: GetCell(row, "classCode"),
                    SubjectCode: GetCell(row, "subjectCode"),
                    SchoolYearCode: GetCell(row, "schoolYearCode")))
                .ToList();

            var teacherRole = await roleManager.FindByNameAsync(SchoolRoles.Teacher) ??
                throw new InvalidOperationException("Teacher role not seeded.");
            var studentRole = await roleManager.FindByNameAsync(SchoolRoles.Student) ??
                throw new InvalidOperationException("Student role not seeded.");
            var parentRole = await roleManager.FindByNameAsync(SchoolRoles.Parent) ??
                throw new InvalidOperationException("Parent role not seeded.");

            int createdUsers = 0, updatedUsers = 0, createdProfiles = 0, createdLinks = 0, createdAssignments = 0, createdEnrollments = 0;

            // Load the full class + school year lookup once so we can derive
            // SchoolYear.StartDate for every newly created StudentEnrollment.EnrolledOn.
            var classList = await dbContext.ClassRooms
                .ToListAsync(cancellationToken);
            var classLookup = classList.ToDictionary(item => item.Code, StringComparer.Ordinal);
            var subjectLookup = await dbContext.Subjects.ToDictionaryAsync(item => item.Code, cancellationToken);
            var schoolYears = await dbContext.SchoolYears
                .ToDictionaryAsync(item => item.Id, cancellationToken);

            var teacherCodeMap = new Dictionary<string, TeacherProfile>(StringComparer.Ordinal);
            foreach (var staged in teacherRows)
            {
                var profile = await EnsureTeacherAsync(staged, teacherRole, teacherCodeMap, command.ActorUserId, command.CorrelationId, cancellationToken);
                if (profile.createdNewUser) createdUsers++; else updatedUsers++;
                if (profile.createdNewProfile) createdProfiles++;
            }
            foreach (var staged in assignmentRows)
            {
                if (!teacherCodeMap.TryGetValue(staged.EmployeeCode, out var teacher))
                {
                    throw new InvalidOperationException($"Teacher {staged.EmployeeCode} not found.");
                }
                if (!classLookup.TryGetValue(staged.ClassCode, out var classroom))
                {
                    throw new InvalidOperationException($"Class {staged.ClassCode} not found.");
                }
                if (!subjectLookup.TryGetValue(staged.SubjectCode, out var subject))
                {
                    throw new InvalidOperationException($"Subject {staged.SubjectCode} not found.");
                }
                var exists = await dbContext.TeacherClassSubjectAssignments
                    .AnyAsync(item => item.TeacherProfileId == teacher.Id && item.ClassId == classroom.Id &&
                        item.SubjectId == subject.Id, cancellationToken);
                if (exists) continue;
                dbContext.TeacherClassSubjectAssignments.Add(new TeacherClassSubjectAssignment
                {
                    Id = Guid.NewGuid(),
                    TeacherProfileId = teacher.Id,
                    ClassId = classroom.Id,
                    SubjectId = subject.Id,
                    SchoolYearId = classroom.SchoolYearId,
                    IsActive = true,
                    CreatedAtUtc = timeProvider.GetUtcNow(),
                    RowVersion = [],
                });
                createdAssignments++;
            }

            var studentCodeMap = new Dictionary<string, StudentProfile>(StringComparer.Ordinal);
            foreach (var staged in studentRows)
            {
                var profile = await EnsureStudentAsync(staged, studentRole, studentCodeMap, command.ActorUserId, command.CorrelationId, cancellationToken);
                if (profile.createdNewUser) createdUsers++; else updatedUsers++;
                if (profile.createdNewProfile) createdProfiles++;
                if (!classLookup.TryGetValue(staged.ClassCode, out var classroom))
                {
                    throw new InvalidOperationException($"Class {staged.ClassCode} not found for student {staged.Code}.");
                }
                if (!schoolYears.TryGetValue(classroom.SchoolYearId, out var schoolYear))
                {
                    throw new InvalidOperationException($"School year for class {staged.ClassCode} not found.");
                }
                var enrollmentExists = await dbContext.StudentEnrollments
                    .AnyAsync(item => item.StudentProfileId == profile.Profile.Id && item.SchoolYearId == classroom.SchoolYearId, cancellationToken);
                if (!enrollmentExists)
                {
                    dbContext.StudentEnrollments.Add(new StudentEnrollment
                    {
                        Id = Guid.NewGuid(),
                        StudentProfileId = profile.Profile.Id,
                        ClassId = classroom.Id,
                        SchoolYearId = classroom.SchoolYearId,
                        // EnrolledOn is sourced from the referenced SchoolYear.StartDate, never from
                        // StudentProfile.DateOfBirth — this is the deterministic rule documented in
                        // AGENTS.md for workbooks that carry no enrolment-date column.
                        EnrolledOn = schoolYear.StartDate,
                        LeftOn = null,
                        CreatedAtUtc = timeProvider.GetUtcNow(),
                        RowVersion = [],
                    });
                    createdEnrollments++;
                }
            }

            var parentCodeMap = new Dictionary<string, ParentProfile>(StringComparer.Ordinal);
            foreach (var staged in parentRows)
            {
                var profile = await EnsureParentAsync(staged, parentRole, parentCodeMap, command.ActorUserId, command.CorrelationId, cancellationToken);
                if (profile.createdNewUser) createdUsers++; else updatedUsers++;
                if (profile.createdNewProfile) createdProfiles++;
            }
            foreach (var staged in linkRows)
            {
                if (!parentCodeMap.TryGetValue(staged.ParentCode, out var parent))
                {
                    throw new InvalidOperationException($"Parent {staged.ParentCode} not found.");
                }
                if (!studentCodeMap.TryGetValue(staged.StudentCode, out var student))
                {
                    throw new InvalidOperationException($"Student {staged.StudentCode} not found.");
                }
                var relationship = ParseRelationship(staged.Relationship);
                var exists = await dbContext.ParentStudentLinks
                    .AnyAsync(item => item.ParentProfileId == parent.Id && item.StudentProfileId == student.Id, cancellationToken);
                if (exists) continue;
                dbContext.ParentStudentLinks.Add(new ParentStudentLink
                {
                    Id = Guid.NewGuid(),
                    ParentProfileId = parent.Id,
                    StudentProfileId = student.Id,
                    Relationship = relationship,
                    IsPrimaryContact = ParseBool(staged.IsPrimaryContact),
                    IsActive = true,
                    CreatedAtUtc = timeProvider.GetUtcNow(),
                    RowVersion = [],
                });
                createdLinks++;
            }

            dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = command.ActorUserId,
                EventType = $"excelImportCommitted:batch={batch.Id}:rows={batch.RowCount}",
                SubjectUserId = null,
                OccurredAtUtc = timeProvider.GetUtcNow(),
                CorrelationId = command.CorrelationId,
            });

            batch.Status = ImportBatchStatus.Committed;
            batch.CommittedAtUtc = timeProvider.GetUtcNow();
            batch.CreatedUserCount = createdUsers;
            batch.UpdatedUserCount = updatedUsers;
            batch.CreatedProfileCount = createdProfiles;
            batch.CreatedLinkCount = createdLinks;
            batch.CreatedAssignmentCount = createdAssignments;
            batch.CreatedEnrollmentCount = createdEnrollments;

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Imported batch {BatchId} committed: users={Users} profiles={Profiles} links={Links} assignments={Assignments} enrollments={Enrollments}.",
                batch.Id, createdUsers, createdProfiles, createdLinks, createdAssignments, createdEnrollments);
            return OperationResult<ImportBatchSummary>.Success(ToSummary(batch));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Excel import commit failed for {BatchId}.", batch.Id);
            await transaction.RollbackAsync(CancellationToken.None);
            batch.Status = ImportBatchStatus.Failed;
            batch.FailureReason = exception.GetType().Name;
            await dbContext.SaveChangesAsync(CancellationToken.None);
            return OperationResult<ImportBatchSummary>.Failure("importCommitFailed");
        }
    }

    public async Task<OperationResult<ImportBatchSummary>> GetAsync(Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await dbContext.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == batchId, cancellationToken);
        if (batch is null)
        {
            return OperationResult<ImportBatchSummary>.Failure("importBatchNotFound");
        }
        return OperationResult<ImportBatchSummary>.Success(ToSummary(batch));
    }

    // ------------------------------------------------------------------
    // Parsing + validation helpers
    // ------------------------------------------------------------------

    private static List<ImportBatchRow> ParseStagingRows(XLWorkbook workbook, Guid batchId)
    {
        var rows = new List<ImportBatchRow>();
        foreach (var sheet in ExcelTemplateBuilder.Sheets)
        {
            if (!workbook.Worksheets.TryGetWorksheet(sheet, out var ws))
            {
                throw new WorkbookHeaderMissingException(sheet, $"Sheet '{sheet}' missing.");
            }

            var headerCells = ws.Row(1).CellsUsed().ToList();
            var headers = headerCells.ToDictionary(cell => cell.GetString().Trim(), cell => cell.Address.ColumnNumber, StringComparer.Ordinal);
            if (headers.Count == 0)
            {
                throw new WorkbookHeaderMissingException(sheet, $"No header cells found on sheet '{sheet}'.");
            }
            var expected = ExcelTemplateBuilder.Headers[sheet];
            foreach (var expectedHeader in expected)
            {
                if (!headers.ContainsKey(expectedHeader))
                {
                    throw new WorkbookHeaderMissingException(sheet, $"Missing header '{expectedHeader}' on sheet '{sheet}'. Found headers: [{string.Join(", ", headers.Keys)}].");
                }
            }

            var dataRows = ws.RowsUsed().Where(row => row.RowNumber() > 1).ToList();
            foreach (var dataRow in dataRows)
            {
                var rowNumber = dataRow.RowNumber();
                var payload = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (var header in expected)
                {
                    var columnNumber = headers[header];
                    var cell = dataRow.Cell(columnNumber);
                    payload[header] = cell.GetString().Trim();
                }

                rows.Add(new ImportBatchRow
                {
                    Id = Guid.NewGuid(),
                    BatchId = batchId,
                    SheetName = sheet,
                    SheetRowNumber = rowNumber,
                    ReferenceCode = ReferenceCodeFor(sheet, payload),
                    ErrorCode = "staging",
                    ErrorMessage = SerializePayload(payload),
                    ColumnName = null,
                });
            }
        }
        return rows;
    }

    private static string? ReferenceCodeFor(string sheet, IDictionary<string, string> payload) => sheet switch
    {
        ExcelTemplateBuilder.SheetStudents => payload.TryGetValue("studentCode", out var sc) ? sc : null,
        ExcelTemplateBuilder.SheetParents => payload.TryGetValue("parentCode", out var pc) ? pc : null,
        ExcelTemplateBuilder.SheetTeachers => payload.TryGetValue("employeeCode", out var ec) ? ec : null,
        ExcelTemplateBuilder.SheetParentStudentLinks => payload.TryGetValue("parentCode", out var plc) && payload.TryGetValue("studentCode", out var slc) ? $"{plc}/{slc}" : null,
        ExcelTemplateBuilder.SheetTeacherAssignments => payload.TryGetValue("employeeCode", out var tec) && payload.TryGetValue("classCode", out var cc) ? $"{tec}/{cc}" : null,
        _ => null,
    };

    private static string SerializePayload(IDictionary<string, string> payload) =>
        string.Join('|', payload.Select(kvp => $"{kvp.Key}={kvp.Value}"));

    private static string GetCell(ImportBatchRow row, string column)
    {
        var prefix = $"{column}=";
        foreach (var segment in row.ErrorMessage.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment.StartsWith(prefix, StringComparison.Ordinal))
            {
                return segment[prefix.Length..];
            }
        }
        return string.Empty;
    }

    private static void ValidateStagedRows(
        IReadOnlyList<ImportBatchRow> rows,
        List<ImportRowError> errors,
        IReadOnlyDictionary<string, Guid> classSchoolYearIds,
        IReadOnlyDictionary<string, SchoolYear> schoolYearCodeLookup,
        IReadOnlyDictionary<Guid, string> schoolYearIdToCode,
        HashSet<string> subjectSet,
        HashSet<string> schoolYearSet,
        HashSet<string> studentCodesInDb,
        HashSet<string> parentCodesInDb,
        HashSet<string> teacherCodesInDb,
        HashSet<string> normalizedEmailsInDb)
    {
        var seenStudentCodes = new HashSet<string>(StringComparer.Ordinal);
        var seenParentCodes = new HashSet<string>(StringComparer.Ordinal);
        var seenTeacherCodes = new HashSet<string>(StringComparer.Ordinal);
        var seenTeacherEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenStudentClassPairs = new HashSet<string>(StringComparer.Ordinal);
        var seenTeacherAssignmentTriples = new HashSet<string>(StringComparer.Ordinal);

        foreach (var row in rows)
        {
            var sheet = row.SheetName;
            switch (sheet)
            {
                case ExcelTemplateBuilder.SheetStudents:
                    ValidateStudent(row, seenStudentCodes, classSchoolYearIds, studentCodesInDb, normalizedEmailsInDb, seenStudentClassPairs, errors);
                    break;
                case ExcelTemplateBuilder.SheetParents:
                    ValidateParent(row, seenParentCodes, normalizedEmailsInDb, errors);
                    break;
                case ExcelTemplateBuilder.SheetTeachers:
                    ValidateTeacher(row, seenTeacherCodes, seenTeacherEmails, normalizedEmailsInDb, errors);
                    break;
                case ExcelTemplateBuilder.SheetParentStudentLinks:
                    ValidateParentStudentLink(row, seenParentCodes, seenStudentCodes, parentCodesInDb, studentCodesInDb, errors);
                    break;
                case ExcelTemplateBuilder.SheetTeacherAssignments:
                    ValidateTeacherAssignment(row, seenTeacherCodes, seenTeacherAssignmentTriples, subjectSet, schoolYearSet, schoolYearCodeLookup, schoolYearIdToCode, classSchoolYearIds, errors);
                    break;
            }
        }
    }

    private static void ValidateStudent(
        ImportBatchRow row,
        HashSet<string> seenStudentCodes,
        IReadOnlyDictionary<string, Guid> classSchoolYearIds,
        HashSet<string> studentCodesInDb,
        HashSet<string> normalizedEmailsInDb,
        HashSet<string> seenStudentClassPairs,
        List<ImportRowError> errors)
    {
        var code = GetCell(row, "studentCode");
        var fullName = GetCell(row, "fullName");
        var dob = GetCell(row, "dateOfBirth");
        var classCode = GetCell(row, "classCode");
        var email = GetCell(row, "email");
        if (string.IsNullOrWhiteSpace(code)) errors.Add(MissingField(row, "studentCode"));
        else if (!seenStudentCodes.Add(code)) errors.Add(DuplicateInBatch(row, "studentCode", code));
        else if (studentCodesInDb.Contains(code)) errors.Add(DuplicateAgainstDatabase(row, "studentCode", code));
        if (string.IsNullOrWhiteSpace(fullName)) errors.Add(MissingField(row, "fullName"));
        if (string.IsNullOrWhiteSpace(classCode)) errors.Add(MissingField(row, "classCode"));
        else if (!classSchoolYearIds.ContainsKey(classCode)) errors.Add(ReferenceNotFound(row, "classCodeNotFound", $"Mã lớp '{classCode}' không tồn tại.", classCode));
        if (!string.IsNullOrWhiteSpace(dob) && ParseDate(dob) is null)
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "invalidDateFormat", $"Ngày sinh '{dob}' không đúng định dạng YYYY-MM-DD.", "dateOfBirth", code));
        }
        if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "invalidEmailFormat", $"Email '{email}' không hợp lệ.", "email", code));
        }
        else if (!string.IsNullOrWhiteSpace(email) && normalizedEmailsInDb.Contains(email.Trim().ToUpperInvariant()))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "emailInUse", $"Email '{email}' đã được sử dụng bởi một tài khoản khác.", "email", code));
        }
        if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(classCode) &&
            classSchoolYearIds.ContainsKey(classCode) && !seenStudentClassPairs.Add($"{code}|{classCode}"))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "duplicateStudentClassAssignment",
                $"Học sinh '{code}' đã được gán lớp '{classCode}' trong workbook.",
                "classCode", code));
        }
    }

    private static void ValidateParent(
        ImportBatchRow row,
        HashSet<string> seenParentCodes,
        HashSet<string> normalizedEmailsInDb,
        List<ImportRowError> errors)
    {
        var code = GetCell(row, "parentCode");
        var fullName = GetCell(row, "fullName");
        var email = GetCell(row, "email");
        if (string.IsNullOrWhiteSpace(code)) errors.Add(MissingField(row, "parentCode"));
        else if (!seenParentCodes.Add(code)) errors.Add(DuplicateInBatch(row, "parentCode", code));
        if (string.IsNullOrWhiteSpace(fullName)) errors.Add(MissingField(row, "fullName"));
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "missingRequiredField", "Phụ huynh cần có email hợp lệ.", "email", code));
        }
        else if (!IsValidEmail(email))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "invalidEmailFormat", $"Email '{email}' không hợp lệ.", "email", code));
        }
        else if (normalizedEmailsInDb.Contains(email.Trim().ToUpperInvariant()))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "emailInUse", $"Email '{email}' đã được sử dụng bởi một tài khoản khác.", "email", code));
        }
    }

    private static void ValidateTeacher(
        ImportBatchRow row,
        HashSet<string> seenTeacherCodes,
        HashSet<string> seenTeacherEmails,
        HashSet<string> normalizedEmailsInDb,
        List<ImportRowError> errors)
    {
        var code = GetCell(row, "employeeCode");
        var fullName = GetCell(row, "fullName");
        var email = GetCell(row, "email");
        if (string.IsNullOrWhiteSpace(code)) errors.Add(MissingField(row, "employeeCode"));
        else if (!seenTeacherCodes.Add(code)) errors.Add(DuplicateInBatch(row, "employeeCode", code));
        if (string.IsNullOrWhiteSpace(fullName)) errors.Add(MissingField(row, "fullName"));
        if (string.IsNullOrWhiteSpace(email))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "missingRequiredField", "Giáo viên cần có email hợp lệ.", "email", code));
        }
        else if (!IsValidEmail(email))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "invalidEmailFormat", $"Email '{email}' không hợp lệ.", "email", code));
        }
        else if (!seenTeacherEmails.Add(email))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "duplicateEmailInBatch", $"Email '{email}' bị trùng trong workbook.", "email", code));
        }
        else if (normalizedEmailsInDb.Contains(email.Trim().ToUpperInvariant()))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "emailInUse", $"Email '{email}' đã được sử dụng bởi một tài khoản khác.", "email", code));
        }
    }

    private static void ValidateParentStudentLink(
        ImportBatchRow row,
        HashSet<string> seenParentCodes,
        HashSet<string> seenStudentCodes,
        HashSet<string> parentCodesInDb,
        HashSet<string> studentCodesInDb,
        List<ImportRowError> errors)
    {
        var parent = GetCell(row, "parentCode");
        var student = GetCell(row, "studentCode");
        var relationship = GetCell(row, "relationship");
        if (string.IsNullOrWhiteSpace(parent)) errors.Add(MissingField(row, "parentCode"));
        else if (!seenParentCodes.Contains(parent) && !parentCodesInDb.Contains(parent))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "parentOrStudentCodeMissing",
                $"Mã phụ huynh '{parent}' không tồn tại trong workbook hoặc cơ sở dữ liệu.",
                "parentCode", $"{parent}/{student}"));
        }
        if (string.IsNullOrWhiteSpace(student)) errors.Add(MissingField(row, "studentCode"));
        else if (!seenStudentCodes.Contains(student) && !studentCodesInDb.Contains(student))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "parentOrStudentCodeMissing",
                $"Mã học sinh '{student}' không tồn tại trong workbook hoặc cơ sở dữ liệu.",
                "studentCode", $"{parent}/{student}"));
        }
        if (!string.IsNullOrWhiteSpace(relationship) && Array.IndexOf(AllowedRelationships, relationship) < 0)
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "invalidEnumValue",
                $"Giá trị quan hệ '{relationship}' không hợp lệ (hợp lệ: father, mother, guardian, other).",
                "relationship", $"{parent}/{student}"));
        }
    }

    private static void ValidateTeacherAssignment(
        ImportBatchRow row,
        HashSet<string> seenTeacherCodes,
        HashSet<string> seenTeacherAssignmentTriples,
        HashSet<string> subjectSet,
        HashSet<string> schoolYearSet,
        IReadOnlyDictionary<string, SchoolYear> schoolYearCodeLookup,
        IReadOnlyDictionary<Guid, string> schoolYearIdToCode,
        IReadOnlyDictionary<string, Guid> classSchoolYearIds,
        List<ImportRowError> errors)
    {
        var teacher = GetCell(row, "employeeCode");
        var classCode = GetCell(row, "classCode");
        var subjectCode = GetCell(row, "subjectCode");
        var schoolYear = GetCell(row, "schoolYearCode");
        if (string.IsNullOrWhiteSpace(teacher)) errors.Add(MissingField(row, "employeeCode"));
        else if (!seenTeacherCodes.Contains(teacher))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "missingTeacherReference",
                $"Mã giáo viên '{teacher}' chưa xuất hiện trong sheet Teachers.",
                "employeeCode", $"{teacher}/{classCode}"));
        }
        if (string.IsNullOrWhiteSpace(classCode)) errors.Add(MissingField(row, "classCode"));
        else if (!classSchoolYearIds.ContainsKey(classCode))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "classCodeNotFound",
                $"Mã lớp '{classCode}' không tồn tại.",
                "classCode", $"{teacher}/{classCode}"));
        }
        if (string.IsNullOrWhiteSpace(subjectCode)) errors.Add(MissingField(row, "subjectCode"));
        else if (!subjectSet.Contains(subjectCode))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "subjectCodeNotFound",
                $"Mã môn học '{subjectCode}' không tồn tại.",
                "subjectCode", $"{teacher}/{classCode}"));
        }
        if (string.IsNullOrWhiteSpace(schoolYear)) errors.Add(MissingField(row, "schoolYearCode"));
        else if (!schoolYearSet.Contains(schoolYear))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "schoolYearCodeNotFound",
                $"Mã năm học '{schoolYear}' không tồn tại.",
                "schoolYearCode", $"{teacher}/{classCode}"));
        }
        else if (classSchoolYearIds.TryGetValue(classCode, out var schoolYearId) &&
                 schoolYearIdToCode.TryGetValue(schoolYearId, out var classYearCode) &&
                 !string.Equals(classYearCode, schoolYear, StringComparison.Ordinal))
        {
            errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                "teacherAssignmentSchoolYearMismatch",
                $"Lớp '{classCode}' thuộc năm học '{classYearCode}' nhưng hàng này tham chiếu '{schoolYear}'.",
                "schoolYearCode", $"{teacher}/{classCode}"));
        }
        if (!string.IsNullOrWhiteSpace(teacher) && !string.IsNullOrWhiteSpace(classCode) &&
            !string.IsNullOrWhiteSpace(subjectCode) && !string.IsNullOrWhiteSpace(schoolYear) &&
            classSchoolYearIds.TryGetValue(classCode, out var ca) && schoolYearIdToCode.TryGetValue(ca, out var caYear) &&
            string.Equals(caYear, schoolYear, StringComparison.Ordinal))
        {
            var triple = $"{teacher}|{classCode}|{subjectCode}|{schoolYear}";
            if (!seenTeacherAssignmentTriples.Add(triple))
            {
                errors.Add(new ImportRowError(row.SheetName, row.SheetRowNumber,
                    "duplicateTeacherAssignmentInBatch",
                    $"Phân công giáo viên '{teacher}' cho lớp '{classCode}'/môn '{subjectCode}' đã xuất hiện trong workbook.",
                    "employeeCode", $"{teacher}/{classCode}"));
            }
        }
        _ = schoolYearCodeLookup;
    }

    private static ImportRowError MissingField(ImportBatchRow row, string column) =>
        new(row.SheetName, row.SheetRowNumber,
            "missingRequiredField",
            $"Thiếu trường bắt buộc '{column}'.",
            column,
            row.ReferenceCode);

    private static ImportRowError DuplicateInBatch(ImportBatchRow row, string column, string code) =>
        new(row.SheetName, row.SheetRowNumber,
            "duplicateCodeInBatch",
            $"Mã '{code}' bị trùng trong workbook.",
            column,
            code);

    private static ImportRowError DuplicateAgainstDatabase(ImportBatchRow row, string column, string code) =>
        new(row.SheetName, row.SheetRowNumber,
            "duplicateCodeAgainstDatabase",
            $"Mã '{code}' đã tồn tại trong cơ sở dữ liệu.",
            column,
            code);

    private static ImportRowError ReferenceNotFound(ImportBatchRow row, string code, string message, string reference) =>
        new(row.SheetName, row.SheetRowNumber,
            code,
            message,
            null,
            reference);

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        System.Text.RegularExpressions.Regex.IsMatch(email,
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    private static DateOnly? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }
        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            return date;
        }
        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }
        return null;
    }

    private static bool ParseBool(string value) =>
        bool.TryParse(value, out var result) && result;

    private static GuardianRelationship ParseRelationship(string value) => value switch
    {
        "father" => GuardianRelationship.Father,
        "mother" => GuardianRelationship.Mother,
        "guardian" => GuardianRelationship.Guardian,
        _ => GuardianRelationship.Other,
    };

    private static ImportBatchSummary ToSummary(ImportBatch batch) =>
        new(batch.Id,
            batch.FileName,
            batch.FileSizeBytes,
            batch.Status.ToString(),
            batch.HasBlockingErrors,
            batch.RowCount,
            batch.CreatedUserCount,
            batch.UpdatedUserCount,
            batch.CreatedProfileCount,
            batch.CreatedLinkCount,
            batch.CreatedAssignmentCount,
            batch.CreatedEnrollmentCount,
            batch.FailureReason,
            batch.CreatedAtUtc,
            batch.ValidatedAtUtc,
            batch.CommittedAtUtc);

    // ------------------------------------------------------------------
    // Provisioning helpers (user + profile)
    // ------------------------------------------------------------------

    private async Task<(TeacherProfile Profile, bool createdNewUser, bool createdNewProfile)> EnsureTeacherAsync(
        StagedTeacher staged,
        AppRole role,
        Dictionary<string, TeacherProfile> codeMap,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var existingProfile = await dbContext.TeacherProfiles
            .FirstOrDefaultAsync(item => item.EmployeeCode == staged.Code, cancellationToken);
        var user = existingProfile is null
            ? await userManager.FindByEmailAsync(staged.Email)
            : await userManager.FindByIdAsync(existingProfile.UserId.ToString());
        bool createdNewUser = false;
        if (user is null)
        {
            var temporaryPassword = PasswordGenerator.Generate();
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = staged.Email,
                NormalizedUserName = staged.Email.ToUpperInvariant(),
                Email = staged.Email,
                NormalizedEmail = staged.Email.ToUpperInvariant(),
                DisplayName = staged.FullName,
                EmailConfirmed = true,
                IsActive = true,
                MustChangePassword = true,
                // The generated password is consumed exactly once by the Administrator who
                // calls the activation-ticket endpoint. The plaintext value is never exported
                // or returned from a batch endpoint.
                TemporaryPasswordExpiresAtUtc = timeProvider.GetUtcNow().AddHours(24),
                CreatedAtUtc = timeProvider.GetUtcNow(),
                UpdatedAtUtc = timeProvider.GetUtcNow(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
            };
            var create = await userManager.CreateAsync(user, temporaryPassword);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể tạo tài khoản giáo viên cho {staged.Email}: {string.Join(", ", create.Errors.Select(item => item.Description))}");
            }
            createdNewUser = true;
            await userManager.AddToRoleAsync(user, SchoolRoles.Teacher);
            dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = $"teacherImported:code={staged.Code}",
                SubjectUserId = user.Id,
                OccurredAtUtc = timeProvider.GetUtcNow(),
                CorrelationId = correlationId,
            });
        }
        else
        {
            user.DisplayName = staged.FullName;
            user.UpdatedAtUtc = timeProvider.GetUtcNow();
            await userManager.UpdateAsync(user);
        }

        if (existingProfile is null)
        {
            existingProfile = new TeacherProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                EmployeeCode = staged.Code,
                IsActive = true,
            };
            dbContext.TeacherProfiles.Add(existingProfile);
            await dbContext.SaveChangesAsync(cancellationToken);
            codeMap[staged.Code] = existingProfile;
            return (existingProfile, createdNewUser, true);
        }
        codeMap[staged.Code] = existingProfile;
        return (existingProfile, createdNewUser, false);
    }

    private async Task<(StudentProfile Profile, bool createdNewUser, bool createdNewProfile)> EnsureStudentAsync(
        StagedStudent staged,
        AppRole role,
        Dictionary<string, StudentProfile> codeMap,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var existingProfile = await dbContext.StudentProfiles
            .FirstOrDefaultAsync(item => item.StudentCode == staged.Code, cancellationToken);
        var userName = string.IsNullOrWhiteSpace(staged.UserName) ? staged.Code : staged.UserName;
        var user = existingProfile is null
            ? await userManager.FindByNameAsync(userName)
            : await userManager.FindByIdAsync(existingProfile.UserId.ToString());
        var dob = ParseDate(staged.DateOfBirth);
        bool createdNewUser = false;
        if (user is null)
        {
            var temporaryPassword = PasswordGenerator.Generate();
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = string.IsNullOrWhiteSpace(staged.Email) ? null : staged.Email,
                NormalizedEmail = string.IsNullOrWhiteSpace(staged.Email) ? null : staged.Email.ToUpperInvariant(),
                DisplayName = staged.FullName,
                EmailConfirmed = true,
                IsActive = true,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = timeProvider.GetUtcNow().AddHours(24),
                CreatedAtUtc = timeProvider.GetUtcNow(),
                UpdatedAtUtc = timeProvider.GetUtcNow(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
            };
            var create = await userManager.CreateAsync(user, temporaryPassword);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể tạo tài khoản học sinh {userName}: {string.Join(", ", create.Errors.Select(item => item.Description))}");
            }
            createdNewUser = true;
            await userManager.AddToRoleAsync(user, SchoolRoles.Student);
            dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = $"studentImported:code={staged.Code}",
                SubjectUserId = user.Id,
                OccurredAtUtc = timeProvider.GetUtcNow(),
                CorrelationId = correlationId,
            });
        }
        else
        {
            user.DisplayName = staged.FullName;
            user.UpdatedAtUtc = timeProvider.GetUtcNow();
            await userManager.UpdateAsync(user);
        }

        if (existingProfile is null)
        {
            existingProfile = new StudentProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                StudentCode = staged.Code,
                IsActive = true,
                DateOfBirth = dob,
            };
            dbContext.StudentProfiles.Add(existingProfile);
            await dbContext.SaveChangesAsync(cancellationToken);
            codeMap[staged.Code] = existingProfile;
            return (existingProfile, createdNewUser, true);
        }
        if (existingProfile.DateOfBirth != dob)
        {
            existingProfile.DateOfBirth = dob;
        }
        codeMap[staged.Code] = existingProfile;
        return (existingProfile, createdNewUser, false);
    }

    private async Task<(ParentProfile Profile, bool createdNewUser, bool createdNewProfile)> EnsureParentAsync(
        StagedParent staged,
        AppRole role,
        Dictionary<string, ParentProfile> codeMap,
        Guid actorUserId,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var existingProfile = await dbContext.ParentProfiles
            .FirstOrDefaultAsync(item => item.ParentCode == staged.Code, cancellationToken);
        var user = existingProfile is null
            ? await userManager.FindByEmailAsync(staged.Email)
            : await userManager.FindByIdAsync(existingProfile.UserId.ToString());
        bool createdNewUser = false;
        if (user is null)
        {
            var temporaryPassword = PasswordGenerator.Generate();
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = staged.Email,
                NormalizedUserName = staged.Email.ToUpperInvariant(),
                Email = staged.Email,
                NormalizedEmail = staged.Email.ToUpperInvariant(),
                DisplayName = staged.FullName,
                EmailConfirmed = true,
                IsActive = true,
                MustChangePassword = true,
                TemporaryPasswordExpiresAtUtc = timeProvider.GetUtcNow().AddHours(24),
                CreatedAtUtc = timeProvider.GetUtcNow(),
                UpdatedAtUtc = timeProvider.GetUtcNow(),
                SecurityStamp = Guid.NewGuid().ToString("N"),
            };
            var create = await userManager.CreateAsync(user, temporaryPassword);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Không thể tạo tài khoản phụ huynh cho {staged.Email}: {string.Join(", ", create.Errors.Select(item => item.Description))}");
            }
            createdNewUser = true;
            await userManager.AddToRoleAsync(user, SchoolRoles.Parent);
            dbContext.SecurityAuditEvents.Add(new SecurityAuditEvent
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                EventType = $"parentImported:code={staged.Code}",
                SubjectUserId = user.Id,
                OccurredAtUtc = timeProvider.GetUtcNow(),
                CorrelationId = correlationId,
            });
        }
        else
        {
            user.DisplayName = staged.FullName;
            user.UpdatedAtUtc = timeProvider.GetUtcNow();
            await userManager.UpdateAsync(user);
        }

        if (existingProfile is null)
        {
            existingProfile = new ParentProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                ParentCode = staged.Code,
                IsActive = true,
            };
            dbContext.ParentProfiles.Add(existingProfile);
            await dbContext.SaveChangesAsync(cancellationToken);
            codeMap[staged.Code] = existingProfile;
            return (existingProfile, createdNewUser, true);
        }
        codeMap[staged.Code] = existingProfile;
        return (existingProfile, createdNewUser, false);
    }

    private sealed record StagedStudent(
        int RowNumber,
        string Code,
        string FullName,
        string DateOfBirth,
        string ClassCode,
        string UserName,
        string Email);

    private sealed record StagedParent(
        int RowNumber,
        string Code,
        string FullName,
        string Email,
        string Phone);

    private sealed record StagedTeacher(
        int RowNumber,
        string Code,
        string FullName,
        string Email,
        string Phone);

    private sealed record StagedParentStudentLink(
        int RowNumber,
        string ParentCode,
        string StudentCode,
        string Relationship,
        string IsPrimaryContact);

    private sealed record StagedTeacherAssignment(
        int RowNumber,
        string EmployeeCode,
        string ClassCode,
        string SubjectCode,
        string SchoolYearCode);
}

internal sealed class WorkbookHeaderMissingException(string sheetName, string message)
    : Exception(message)
{
    public string SheetName { get; } = sheetName;
}

internal static class WorkbookStructure
{
    public enum ParseKind { Unreadable, Parsed }

    public sealed record ParseResult(ParseKind Kind, XLWorkbook? Workbook);

    public sealed record FormulaHit(bool HasFormula, string? SheetName, string? ColumnName, int RowNumber);

    public static ParseResult Parse(byte[] content, ILogger logger, string fileName)
    {
        try
        {
            return new ParseResult(ParseKind.Parsed, new XLWorkbook(new MemoryStream(content)));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "XLWorkbook direct construction failed for {FileName}; trying ZIP fallback.", fileName);
            try
            {
                using var zipStream = new MemoryStream(content);
                using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
                return new ParseResult(ParseKind.Parsed, CreateWorkbookFromZipArchive(archive));
            }
            catch (Exception fallbackEx)
            {
                logger.LogWarning(fallbackEx, "ZIP fallback failed for {FileName}.", fileName);
                return new ParseResult(ParseKind.Unreadable, null);
            }
        }
    }

    public static FormulaHit HasFormula(XLWorkbook workbook)
    {
        foreach (var sheet in workbook.Worksheets)
        {
            foreach (var row in sheet.RowsUsed())
            {
                foreach (var cell in row.CellsUsed())
                {
                    if (cell.HasFormula)
                    {
                        return new FormulaHit(true, sheet.Name, cell.Address.ColumnLetter, row.RowNumber());
                    }
                }
            }
        }
        return new FormulaHit(false, null, null, 0);
    }

    private static XLWorkbook CreateWorkbookFromZipArchive(System.IO.Compression.ZipArchive archive)
    {
        var entries = archive.Entries.ToDictionary(e => e.FullName, e =>
        {
            using var ms = new MemoryStream();
            e.Open().CopyTo(ms);
            return ms.ToArray();
        });

        var requiredEntries = new[]
        {
            "[Content_Types].xml",
            "xl/workbook.xml",
            "xl/_rels/workbook.xml.rels",
        };
        foreach (var required in requiredEntries)
        {
            if (!entries.ContainsKey(required))
                throw new InvalidOperationException($"Missing required ZIP entry: {required}");
        }

        var newZip = new MemoryStream();
        using (var newArchive = new System.IO.Compression.ZipArchive(newZip, System.IO.Compression.ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var kvp in entries)
            {
                var newEntry = newArchive.CreateEntry(kvp.Key, System.IO.Compression.CompressionLevel.Optimal);
                using var ew = newEntry.Open();
                ew.Write(kvp.Value);
            }
        }
        newZip.Position = 0;
        return new XLWorkbook(newZip);
    }
}
