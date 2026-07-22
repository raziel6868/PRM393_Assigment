namespace MyFSchool.Domain.School;

public class AnnouncementReadState
{
    public Guid Id { get; set; }

    public Guid FeedPostId { get; set; }

    public Guid UserId { get; set; }

    public DateTime ReadAtUtc { get; set; }

    public FeedPost FeedPost { get; set; } = null!;
}
