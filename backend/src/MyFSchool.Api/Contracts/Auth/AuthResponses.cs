namespace MyFSchool.Api.Contracts.Auth;

public sealed record AuthSessionResponse(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool PasswordChangeRequired,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAtUtc);

public sealed record SessionContextResponse(
    Guid UserId,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool PasswordChangeRequired);

public sealed record ProvisionedUserResponse(
    Guid UserId,
    string DisplayName,
    string UserName,
    string? Email,
    IReadOnlyList<string> Roles,
    string TemporaryPassword,
    DateTimeOffset TemporaryPasswordExpiresAtUtc);
