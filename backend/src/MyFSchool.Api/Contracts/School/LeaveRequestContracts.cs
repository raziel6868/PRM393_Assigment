using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.School;

public sealed record SubmitLeaveRequestBody(
    Guid StudentProfileId,
    [param: Required(ErrorMessage = "Vui lòng nhập ngày bắt đầu.")] DateOnly StartDate,
    [param: Required(ErrorMessage = "Vui lòng nhập ngày kết thúc.")] DateOnly EndDate,
    [param: Required(ErrorMessage = "Vui lòng chọn buổi bắt đầu.")] string StartSession,
    [param: Required(ErrorMessage = "Vui lòng chọn buổi kết thúc.")] string EndSession,
    [param: Required(ErrorMessage = "Vui lòng chọn lý do.")] string ReasonCategory,
    [param: Required(ErrorMessage = "Vui lòng nhập nội dung đơn."), StringLength(500, MinimumLength = 20, ErrorMessage = "Nội dung đơn phải từ 20 đến 500 ký tự.")]
    string Reason);

public sealed record CancelLeaveRequestBody(
    [param: Required(ErrorMessage = "Thiếu phiên bản đơn.")] string RowVersion);

public sealed record DecideLeaveRequestBody(
    bool Approve,
    string? DecisionNote,
    [param: Required(ErrorMessage = "Thiếu phiên bản đơn.")] string RowVersion);

public sealed record LeaveRequestResponse(
    Guid Id,
    Guid StudentProfileId,
    Guid RequesterUserId,
    DateOnly StartDate,
    DateOnly EndDate,
    string StartSession,
    string EndSession,
    string ReasonCategory,
    string Reason,
    string? DecisionNote,
    Guid? ReviewerUserId,
    DateTimeOffset? ReviewedAtUtc,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DecidedAtUtc,
    string RowVersion);

public sealed record LeaveRequestPageResponse(
    IReadOnlyList<LeaveRequestResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);