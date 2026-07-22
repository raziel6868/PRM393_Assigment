using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.Relationships;

public sealed record CreateTeacherProfileRequest(
    Guid UserId,
    [param: Required(ErrorMessage = "Vui lòng nhập mã giáo viên."), StringLength(50, MinimumLength = 1, ErrorMessage = "Mã giáo viên phải có từ 1 đến 50 ký tự."), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", ErrorMessage = "Mã giáo viên chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    string EmployeeCode);

public sealed record CreateStudentProfileRequest(
    Guid UserId,
    [param: Required(ErrorMessage = "Vui lòng nhập mã học sinh."), StringLength(50, MinimumLength = 1, ErrorMessage = "Mã học sinh phải có từ 1 đến 50 ký tự."), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", ErrorMessage = "Mã học sinh chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    string StudentCode);

public sealed record CreateParentProfileRequest(
    Guid UserId,
    [param: Required(ErrorMessage = "Vui lòng nhập mã phụ huynh."), StringLength(50, MinimumLength = 1, ErrorMessage = "Mã phụ huynh phải có từ 1 đến 50 ký tự."), RegularExpression(@"^[A-Za-z0-9][A-Za-z0-9._-]*$", ErrorMessage = "Mã phụ huynh chỉ gồm chữ, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
    string ParentCode);

public sealed record TeacherProfileResponse(Guid Id, Guid UserId, string EmployeeCode, bool IsActive);

public sealed record StudentProfileResponse(Guid Id, Guid UserId, string StudentCode, bool IsActive);

public sealed record ParentProfileResponse(Guid Id, Guid UserId, string ParentCode, bool IsActive);

public sealed record CreateParentStudentLinkRequest(
    Guid ParentProfileId,
    Guid StudentProfileId,
    [param: Required(ErrorMessage = "Vui lòng chọn mối quan hệ.")]
    string Relationship,
    bool IsPrimaryContact);

public sealed record UpdateParentStudentLinkRequest(
    [param: Required(ErrorMessage = "Vui lòng chọn mối quan hệ.")]
    string Relationship,
    bool IsPrimaryContact,
    bool IsActive,
    [param: Required(ErrorMessage = "Thiếu phiên bản liên kết.")]
    string RowVersion);

public sealed record ParentStudentLinkResponse(
    Guid Id,
    Guid ParentProfileId,
    Guid StudentProfileId,
    string Relationship,
    bool IsPrimaryContact,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    string RowVersion);

public sealed record LinkedChildResponse(
    Guid StudentProfileId,
    Guid UserId,
    string DisplayName,
    string StudentCode,
    string Relationship,
    bool IsPrimaryContact);
