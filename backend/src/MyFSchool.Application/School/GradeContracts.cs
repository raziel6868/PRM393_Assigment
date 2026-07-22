using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record GradeResult(
    Guid Id,
    Guid AssessmentId,
    string AssessmentCode,
    string AssessmentName,
    string AssessmentType,
    int Semester,
    Guid SchoolYearId,
    string SchoolYearCode,
    Guid ClassId,
    string ClassCode,
    Guid SubjectId,
    string SubjectName,
    decimal? Score,
    decimal MaxScore,
    string? TeacherComment,
    DateTimeOffset RecordedAtUtc);

public sealed record StudentGradeSummary(
    Guid SchoolYearId,
    string SchoolYearCode,
    int Semester,
    Guid SubjectId,
    string SubjectName,
    decimal? AverageScore,
    int GradeCount,
    IReadOnlyList<GradeResult> Grades);

public sealed record GradePage(
    IReadOnlyList<StudentGradeSummary> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record GradeEntryItem(
    Guid StudentProfileId,
    string StudentCode,
    string StudentDisplayName,
    decimal? ExistingScore,
    decimal? NewScore);

public sealed record SaveGradeEntriesCommand(
    Guid AssessmentId,
    IReadOnlyList<GradeEntryUpdate> Entries,
    Guid ActorUserId,
    string CorrelationId);

public sealed record GradeEntryUpdate(
    Guid StudentProfileId,
    decimal? Score,
    string? TeacherComment,
    byte[]? RowVersion);

public sealed record CreateAssessmentCommand(
    string Code,
    string DisplayName,
    string AssessmentType,
    Guid SchoolYearId,
    int Semester,
    Guid ClassId,
    Guid SubjectId,
    decimal MinScore,
    decimal MaxScore,
    int Weight,
    DateOnly? DueDate,
    bool IsPublished,
    Guid ActorUserId,
    string CorrelationId);

public sealed record AssessmentResult(
    Guid Id,
    string Code,
    string DisplayName,
    string AssessmentType,
    Guid SchoolYearId,
    string SchoolYearCode,
    int Semester,
    Guid ClassId,
    string ClassCode,
    Guid SubjectId,
    string SubjectName,
    decimal MinScore,
    decimal MaxScore,
    int Weight,
    DateOnly? DueDate,
    bool IsPublished,
    string RowVersion);

public sealed record AssessmentRoster(
    Guid Id,
    IReadOnlyList<GradeEntryItem> Students,
    string RowVersion);

public interface IGradeAdministrationService
{
    Task<OperationResult<GradePage>> GetStudentGradesAsync(
        Guid userId,
        Guid? schoolYearId,
        int? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OperationResult<GradePage>> GetParentChildGradesAsync(
        Guid parentUserId,
        Guid studentProfileId,
        Guid? schoolYearId,
        int? semester,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<OperationResult<IReadOnlyList<AssessmentRoster>>> GetAssessmentRostersAsync(
        Guid teacherUserId,
        Guid classId,
        Guid? subjectId,
        Guid schoolYearId,
        int semester,
        CancellationToken cancellationToken);

    Task<OperationResult<AssessmentResult>> CreateAssessmentAsync(
        CreateAssessmentCommand command,
        CancellationToken cancellationToken);

    Task<OperationResult<AssessmentRoster>> SaveGradeEntriesAsync(
        SaveGradeEntriesCommand command,
        CancellationToken cancellationToken);
}
