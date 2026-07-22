using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Domain.School;

public class AnnouncementDelivery
{
    public Guid Id { get; set; }

    public Guid FeedPostId { get; set; }

    public Guid RecipientUserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string RecipientDisplayName { get; set; } = string.Empty;

    public DeliveryChannel Channel { get; set; }

    public DeliveryStatus Status { get; set; }

    public DateTime? SentAtUtc { get; set; }

    [MaxLength(500)]
    public string? FailureReason { get; set; }

    public FeedPost FeedPost { get; set; } = null!;
}
