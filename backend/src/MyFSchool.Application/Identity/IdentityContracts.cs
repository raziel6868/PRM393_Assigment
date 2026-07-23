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
    string SecurityStamp,
    TimeSpan Lifetime);

public sealed record ChangeTemporaryPasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string CorrelationId);

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

public sealed record PasswordHelpQuery(
    PasswordHelpStatusFilter Status,
    int Page,
    int PageSize);

public enum PasswordHelpStatusFilter
{
    Pending,
    Resolved,
    Rejected
}

public sealed record PasswordHelpItem(
    Guid RequestId,
    Guid UserId,
    string DisplayName,
    string UserName,
    string? Email,
    PasswordHelpStatusFilter Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    string RowVersion);

public sealed record PasswordHelpPage(
    IReadOnlyList<PasswordHelpItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record IssueTemporaryPasswordCommand(
    Guid UserId,
    bool Confirmed,
    byte[] RequestRowVersion,
    Guid ActorUserId,
    string CorrelationId);

public sealed record IssuedTemporaryPassword(
    Guid UserId,
    string TemporaryPassword,
    DateTimeOffset ExpiresAtUtc);

public sealed record RejectPasswordHelpCommand(
    Guid RequestId,
    bool Confirmed,
    byte[] RequestRowVersion,
    Guid ActorUserId,
    string CorrelationId);

public sealed record OperationResult<T>(T? Value, string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;

    public static OperationResult<T> Success(T value) => new(value, null);

    public static OperationResult<T> Failure(string errorCode) => new(default, errorCode);
}

public sealed record OperationResult
{
    public string? ErrorCode { get; init; }
    public bool IsSuccess => ErrorCode is null;

    public static OperationResult Ok() => new() { ErrorCode = null };
    public static OperationResult Fail(string errorCode) =>
        new() { ErrorCode = errorCode };
}
