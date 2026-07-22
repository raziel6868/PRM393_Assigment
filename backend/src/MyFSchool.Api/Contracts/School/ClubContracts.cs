using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.School;

public sealed record ClubResponse(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Category,
    int? MaxMembers,
    int CurrentMemberCount,
    bool IsActive,
    string RowVersion);

public sealed record ClubDetailResponse(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description,
    string Category,
    int? MaxMembers,
    int CurrentMemberCount,
    bool IsActive,
    string RowVersion,
    string MembershipStatus,
    DateTimeOffset? JoinedAtUtc);

public sealed record ClubPageResponse(
    IReadOnlyList<ClubResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record CreateClubRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập mã CLB."), StringLength(20, MinimumLength = 1)]
    string Code,
    [param: Required(ErrorMessage = "Vui lòng nhập tên CLB."), StringLength(200, MinimumLength = 1)]
    string DisplayName,
    string? Description,
    [param: Required(ErrorMessage = "Vui lòng nhập danh mục."), StringLength(50)]
    string Category,
    int? MaxMembers);

public sealed record UpdateClubRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập tên CLB."), StringLength(200, MinimumLength = 1)]
    string DisplayName,
    string? Description,
    [param: Required(ErrorMessage = "Vui lòng nhập danh mục."), StringLength(50)]
    string Category,
    int? MaxMembers,
    bool IsActive,
    [param: Required(ErrorMessage = "Thiếu phiên bản.")]
    string RowVersion);
