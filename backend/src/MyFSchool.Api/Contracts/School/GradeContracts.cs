using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.School;

public sealed record GradeResponse(
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

public sealed record StudentGradeSummaryResponse(
    Guid SchoolYearId,
    string SchoolYearCode,
    int Semester,
    Guid SubjectId,
    string SubjectName,
    decimal? AverageScore,
    int GradeCount,
    IReadOnlyList<GradeResponse> Grades);

public sealed record GradePageResponse(
    IReadOnlyList<StudentGradeSummaryResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record GradeEntryItemResponse(
    Guid StudentProfileId,
    string StudentCode,
    string StudentDisplayName,
    decimal? ExistingScore,
    decimal? NewScore);

public sealed record AssessmentRosterResponse(
    Guid Id,
    IReadOnlyList<GradeEntryItemResponse> Students,
    string RowVersion);

public sealed record CreateAssessmentRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập mã điểm."), StringLength(50, MinimumLength = 1)]
    string Code,
    [param: Required(ErrorMessage = "Vui lòng nhập tên điểm."), StringLength(200, MinimumLength = 1)]
    string DisplayName,
    [param: Required(ErrorMessage = "Vui lòng chọn loại điểm.")]
    string AssessmentType,
    [param: Required(ErrorMessage = "Vui lòng chọn năm học.")]
    Guid SchoolYearId,
    [Range(1, 3, ErrorMessage = "Học kỳ phải từ 1 đến 3.")]
    int Semester,
    [param: Required(ErrorMessage = "Vui lòng chọn lớp.")]
    Guid ClassId,
    [param: Required(ErrorMessage = "Vui lòng chọn môn học.")]
    Guid SubjectId,
    [Range(0, 10, ErrorMessage = "Điểm tối thiểu phải từ 0 đến 10.")]
    decimal MinScore,
    [Range(0, 10, ErrorMessage = "Điểm tối đa phải từ 0 đến 10.")]
    decimal MaxScore,
    [Range(1, 10, ErrorMessage = "Trọng số phải từ 1 đến 10.")]
    int Weight,
    DateOnly? DueDate,
    bool IsPublished);

public sealed record GradeEntryUpdateRequest(
    [param: Required(ErrorMessage = "Vui lòng chọn học sinh.")]
    Guid StudentProfileId,
    [Range(0, 10, ErrorMessage = "Điểm phải từ 0 đến 10.")]
    decimal? Score,
    [StringLength(2000)]
    string? TeacherComment,
    string? RowVersion);

public sealed record SaveGradeEntriesRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập danh sách điểm.")]
    IReadOnlyList<GradeEntryUpdateRequest> Entries);
