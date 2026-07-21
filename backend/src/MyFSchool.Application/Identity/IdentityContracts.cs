namespace MyFSchool.Application.Identity;

public enum AuthClientType
{
    Web,
    Mobile
}

public sealed record SignInCommand(
    string EmailOrUserName,
    string Password,
    AuthClientType ClientType,
    string CorrelationId);

public sealed record RefreshSessionCommand(
    string RefreshToken,
    AuthClientType ClientType,
    string CorrelationId);

public sealed record LogoutCommand(string RefreshToken, string CorrelationId);

public sealed record AuthSession(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool PasswordChangeRequired,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc);

public sealed record SessionContext(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool PasswordChangeRequired);

public sealed record AccessTokenDescriptor(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool PasswordChangeRequired,
    TimeSpan Lifetime);

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAtUtc);

public sealed record ProvisionUserCommand(
    string DisplayName,
    string UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    Guid ActorUserId,
    string CorrelationId);

public sealed record ProvisionedUser(
    Guid UserId,
    string DisplayName,
    string UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    string TemporaryPassword,
    DateTimeOffset TemporaryPasswordExpiresAtUtc);

public sealed record OperationResult<T>(T? Value, string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;

    public static OperationResult<T> Success(T value) => new(value, null);

    public static OperationResult<T> Failure(string errorCode) => new(default, errorCode);
}
