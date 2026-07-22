using System.ComponentModel.DataAnnotations;

namespace MyFSchool.Domain.School;

public class FeedPost
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public AnnouncementAudience Audience { get; set; }

    public Guid? TargetClassId { get; set; }

    public Guid AuthorUserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string AuthorDisplayName { get; set; } = string.Empty;

    public bool IsPublished { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? PublishedAtUtc { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ClassRoom? TargetClass { get; set; }

    public ICollection<AnnouncementDelivery> Deliveries { get; set; } = [];

    public ICollection<AnnouncementReadState> ReadStates { get; set; } = [];
}
