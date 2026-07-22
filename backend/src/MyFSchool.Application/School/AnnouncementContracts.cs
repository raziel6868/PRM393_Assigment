using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public record AnnouncementListItem(
    Guid Id,
    string Title,
    string Body,
    string Audience,
    string? TargetClassName,
    string AuthorDisplayName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    string? ImageUrl,
    int ReadCount,
    int TotalRecipientCount);

public record AnnouncementDetail(
    Guid Id,
    string Title,
    string Body,
    string Audience,
    string? TargetClassName,
    string AuthorDisplayName,
    DateTime CreatedAtUtc,
    DateTime? PublishedAtUtc,
    string? ImageUrl,
    int ReadCount,
    int TotalRecipientCount,
    byte[] RowVersion);

public record AnnouncementPage(
    IReadOnlyList<AnnouncementListItem> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public record CreateAnnouncementCommand(
    string Title,
    string Body,
    string Audience,
    Guid? TargetClassId,
    string? ImageUrl);

public record PublishAnnouncementCommand(
    Guid Id,
    List<DeliveryChannelInfo> DeliveryChannels,
    byte[] RowVersion);

public record DeliveryChannelInfo(string Channel);

public interface IAnnouncementAdministrationService
{
    Task<OperationResult<AnnouncementDetail>> CreateAsync(CreateAnnouncementCommand command, Guid authorUserId, CancellationToken ct = default);
    Task<OperationResult<AnnouncementDetail>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<OperationResult> PublishAsync(PublishAnnouncementCommand command, Guid authorUserId, CancellationToken ct = default);
}

public interface IAnnouncementQueryService
{
    Task<AnnouncementPage> GetForUserAsync(Guid userId, string userRole, Guid? userProfileId, int page, int pageSize, CancellationToken ct = default);
    Task<OperationResult<AnnouncementDetail>> GetDetailForUserAsync(Guid announcementId, Guid userId, string userRole, Guid? userProfileId, CancellationToken ct = default);
    Task<OperationResult> MarkAsReadAsync(Guid announcementId, Guid userId, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
}
