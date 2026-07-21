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
