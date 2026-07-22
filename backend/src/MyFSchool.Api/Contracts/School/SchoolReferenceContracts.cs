using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.School;

public sealed record CreateSchoolYearRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập mã năm học."), StringLength(20, MinimumLength = 1, ErrorMessage = "Mã năm học phải có từ 1 đến 20 ký tự."), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", ErrorMessage = "Mã năm học chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    string Code,
    [param: Required(ErrorMessage = "Vui lòng nhập tên năm học."), StringLength(200, MinimumLength = 1)]
    string DisplayName,
    [param: Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")] DateOnly StartDate,
    [param: Required(ErrorMessage = "Vui lòng nhập ngày kết thúc.")] DateOnly EndDate);

public sealed record SchoolYearResponse(
    Guid Id,
    string Code,
    string DisplayName,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string RowVersion);

public sealed record CreateClassRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập mã lớp."), StringLength(20, MinimumLength = 1), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    string Code,
    [param: Required(ErrorMessage = "Vui lòng nhập tên lớp."), StringLength(200, MinimumLength = 1)]
    string DisplayName,
    int GradeLevel,
    Guid SchoolYearId,
    Guid? HomeroomTeacherProfileId);

public sealed record UpdateClassRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập tên lớp."), StringLength(200, MinimumLength = 1)]
    string DisplayName,
    int GradeLevel,
    Guid? HomeroomTeacherProfileId,
    bool IsActive,
    [param: Required(ErrorMessage = "Thiếu phiên bản lớp.")]
    string RowVersion);

public sealed record ClassResponse(
    Guid Id,
    string Code,
    string DisplayName,
    int GradeLevel,
    Guid SchoolYearId,
    string SchoolYearCode,
    Guid? HomeroomTeacherProfileId,
    bool IsActive,
    string RowVersion);

public sealed record CreateSubjectRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập mã môn học."), StringLength(20, MinimumLength = 1), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$")]
    string Code,
    [param: Required(ErrorMessage = "Vui lòng nhập tên môn học."), StringLength(200, MinimumLength = 1)]
    string DisplayName);

public sealed record SubjectResponse(
    Guid Id,
    string Code,
    string DisplayName,
    bool IsActive,
    string RowVersion);

public sealed record CreateTeacherAssignmentRequest(
    Guid TeacherProfileId,
    Guid ClassId,
    Guid SubjectId,
    Guid SchoolYearId);

public sealed record TeacherAssignmentResponse(
    Guid Id,
    Guid TeacherProfileId,
    Guid ClassId,
    Guid SubjectId,
    Guid SchoolYearId,
    bool IsActive,
    string RowVersion);

public sealed record CreateStudentEnrollmentRequest(
    Guid StudentProfileId,
    Guid ClassId,
    Guid SchoolYearId,
    DateOnly EnrolledOn);

public sealed record StudentEnrollmentResponse(
    Guid Id,
    Guid StudentProfileId,
    Guid ClassId,
    Guid SchoolYearId,
    DateOnly EnrolledOn,
    DateOnly? LeftOn,
    string RowVersion);

public sealed record StudentClassScopeResponse(
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SchoolYearId,
    string SchoolYearCode);

public sealed record TeacherClassScopeResponse(
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SubjectId,
    string SubjectCode,
    string SubjectDisplayName,
    Guid SchoolYearId,
    string SchoolYearCode,
    bool IsHomeroom);

public sealed record ParentChildClassScopeResponse(
    Guid StudentProfileId,
    Guid StudentUserId,
    string StudentDisplayName,
    string StudentCode,
    Guid ClassId,
    string ClassCode,
    string ClassDisplayName,
    Guid SchoolYearId,
    string SchoolYearCode);
