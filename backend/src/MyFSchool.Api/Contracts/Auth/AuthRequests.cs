using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.Auth;

public sealed record SignInRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập email hoặc tên đăng nhập.")]
    [param: MaxLength(256, ErrorMessage = "Email hoặc tên đăng nhập không được vượt quá 256 ký tự.")]
    string EmailOrUserName,
    [param: Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    [param: MaxLength(256, ErrorMessage = "Mật khẩu không hợp lệ.")]
    string Password,
    [param: Required(ErrorMessage = "Thiếu loại ứng dụng.")]
    string ClientType);

public sealed record RefreshRequest(string ClientType, string? RefreshToken);

public sealed record LogoutRequest(string ClientType, string? RefreshToken);

public sealed record PasswordHelpSubmissionRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập email hoặc mã tài khoản.")]
    [param: MaxLength(256, ErrorMessage = "Email hoặc mã tài khoản không được vượt quá 256 ký tự.")]
    string EmailOrUserName);

public sealed record ChangeTemporaryPasswordRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập mật khẩu tạm hiện tại.")]
    [param: MaxLength(256, ErrorMessage = "Mật khẩu hiện tại không hợp lệ.")]
    string CurrentPassword,
    [param: Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [param: MinLength(12, ErrorMessage = "Mật khẩu mới phải có ít nhất 12 ký tự.")]
    [param: MaxLength(256, ErrorMessage = "Mật khẩu mới không hợp lệ.")]
    string NewPassword,
    [param: Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
    string Confirmation);

public sealed record ProvisionUserRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
    [param: MaxLength(200, ErrorMessage = "Họ và tên không được vượt quá 200 ký tự.")]
    string DisplayName,
    [param: Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
    [param: MaxLength(256, ErrorMessage = "Tên đăng nhập không được vượt quá 256 ký tự.")]
    string UserName,
    [param: EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
    [param: MaxLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
    string? Email,
    [param: Required(ErrorMessage = "Vui lòng chọn ít nhất một vai trò.")]
    IReadOnlyList<string> Roles);
