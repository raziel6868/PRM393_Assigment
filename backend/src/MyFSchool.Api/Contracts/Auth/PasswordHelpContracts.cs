using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.Auth;

public sealed record PasswordHelpAcceptedResponse(string Message);

public sealed record PasswordHelpItemResponse(
    Guid RequestId,
    Guid UserId,
    string DisplayName,
    string UserName,
    string? Email,
    string Status,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    string RowVersion);

public sealed record PasswordHelpPageResponse(
    IReadOnlyList<PasswordHelpItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record IssueTemporaryPasswordRequest(
    bool Confirmed,
    [param: Required(ErrorMessage = "Thiếu phiên bản yêu cầu hỗ trợ.")]
    string RowVersion);

public sealed record IssueTemporaryPasswordResponse(
    Guid UserId,
    string TemporaryPassword,
    DateTimeOffset ExpiresAtUtc);

public sealed record RejectPasswordHelpRequest(
    bool Confirmed,
    [param: Required(ErrorMessage = "Thiếu phiên bản yêu cầu hỗ trợ.")]
    string RowVersion);
