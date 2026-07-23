using MyFSchool.Application.Identity;

namespace MyFSchool.Application.School;

public sealed record AnnouncementEmail(
    string RecipientEmail,
    string Subject,
    string Body);

public interface IAnnouncementEmailSender
{
    Task<OperationResult> SendAsync(
        AnnouncementEmail email,
        CancellationToken cancellationToken = default);
}
