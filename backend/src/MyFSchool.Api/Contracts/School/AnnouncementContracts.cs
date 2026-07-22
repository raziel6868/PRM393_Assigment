namespace MyFSchool.Api.Contracts.School;

public record AnnouncementListItemDto(
    Guid id,
    string title,
    string bodyPreview,
    string audience,
    string? targetClassName,
    string authorDisplayName,
    DateTime createdAtUtc,
    DateTime? publishedAtUtc,
    string? imageUrl,
    int readCount,
    int totalRecipientCount);

public record AnnouncementDetailDto(
    Guid id,
    string title,
    string body,
    string audience,
    string? targetClassName,
    string authorDisplayName,
    DateTime createdAtUtc,
    DateTime? publishedAtUtc,
    string? imageUrl,
    int readCount,
    int totalRecipientCount,
    string rowVersion);

public record CreateAnnouncementRequest(
    string title,
    string body,
    string audience,
    Guid? targetClassId,
    string? imageUrl)
{
    public CreateAnnouncementRequest() : this("", "", "", null, null) { }

    public List<string> Validate()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(title) || title.Length > 100)
            errors.Add("Tiêu đề bắt buộc, tối đa 100 ký tự.");
        if (string.IsNullOrWhiteSpace(body) || body.Length > 4000)
            errors.Add("Nội dung bắt buộc, tối đa 4000 ký tự.");
        if (string.IsNullOrWhiteSpace(audience))
            errors.Add("Đối tượng nhận thông báo bắt buộc.");
        return errors;
    }
}

public record PublishAnnouncementRequest(
    List<DeliveryChannelRequest> deliveryChannels,
    string rowVersion);

public record DeliveryChannelRequest(string channel);

public record AnnouncementListResponse(
    List<AnnouncementListItemDto> items,
    int page,
    int pageSize,
    int totalCount,
    int totalPages);

public record UnreadCountResponse(int unreadCount);
