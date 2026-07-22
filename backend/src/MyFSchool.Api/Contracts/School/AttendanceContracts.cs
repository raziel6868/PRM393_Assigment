using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Api.Contracts.School;

public sealed record AttendanceRosterEntryResponse(
    Guid StudentProfileId,
    string StudentCode,
    string StudentDisplayName,
    string Status,
    string? Note,
    string? RowVersion);

public sealed record AttendanceRosterResponse(
    Guid ClassId,
    string ClassCode,
    DateOnly AttendanceDate,
    string Session,
    IReadOnlyList<AttendanceRosterEntryResponse> Entries);

public sealed record SaveAttendanceRequest(
    [param: Required(ErrorMessage = "Vui lòng nhập ngày điểm danh.")] DateOnly AttendanceDate,
    [param: Required(ErrorMessage = "Vui lòng chọn buổi học.")] string Session,
    [param: Required(ErrorMessage = "Vui lòng cung cấp danh sách điểm danh.")] IReadOnlyList<SaveAttendanceEntryRequest> Entries);

public sealed record SaveAttendanceEntryRequest(
    Guid StudentProfileId,
    [param: Required(ErrorMessage = "Vui lòng chọn trạng thái điểm danh.")] string Status,
    string? Note,
    string? RowVersion);

public sealed record AttendanceSaveResponse(
    Guid ClassId,
    DateOnly AttendanceDate,
    string Session,
    int SavedCount,
    int UnmarkedCount);

public sealed record AttendanceHistoryItemResponse(
    DateOnly AttendanceDate,
    string Session,
    string ClassDisplayName,
    string Status,
    string? Note);

public sealed record AttendanceHistoryPageResponse(
    IReadOnlyList<AttendanceHistoryItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
