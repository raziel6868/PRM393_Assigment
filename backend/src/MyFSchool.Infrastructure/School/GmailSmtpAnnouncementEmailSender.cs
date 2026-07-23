using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MyFSchool.Application.Identity;
using MyFSchool.Application.School;
using MyFSchool.Infrastructure.Configuration;

namespace MyFSchool.Infrastructure.School;

public sealed class GmailSmtpAnnouncementEmailSender(
    IOptions<SmtpOptions> options) : IAnnouncementEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task<OperationResult> SendAsync(
        AnnouncementEmail email,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return OperationResult.Fail("smtpDisabled");
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = email.Subject,
                Body = email.Body,
                IsBodyHtml = false
            };
            message.To.Add(new MailAddress(email.RecipientEmail));

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = string.Equals(
                    _options.Security,
                    "startTls",
                    StringComparison.OrdinalIgnoreCase),
                Credentials = new NetworkCredential(_options.UserName, _options.Password)
            };
            await client.SendMailAsync(message, cancellationToken);
            return OperationResult.Ok();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SmtpException)
        {
            return OperationResult.Fail("smtpDeliveryFailed");
        }
        catch (FormatException)
        {
            return OperationResult.Fail("invalidRecipientEmail");
        }
    }
}
