using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Sending.Mail;

/// <summary>
/// SMTP sender of the campaigns module, built on MailKit. A failure while connecting or logging in means nothing was
/// submitted, so it reports <see cref="CampaignMailOutcome.NotAccepted"/>. Any failure once the message is being
/// submitted reports <see cref="CampaignMailOutcome.Unknown"/>, because the server may already have taken it.
/// </summary>
public sealed class MailKitCampaignMailSender(IOptions<CampaignsMailOptions> options) : ICampaignMailSender
{
    private const string UnsubscribeHeader = "List-Unsubscribe";

    private const string UnsubscribePostHeader = "List-Unsubscribe-Post";

    private const string OneClickValue = "List-Unsubscribe=One-Click";

    private const string ContentLanguageHeader = "Content-Language";

    private readonly CampaignsMailOptions _options = options.Value;

    public async Task<CampaignMailOutcome> SendAsync(CampaignMailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var mime = BuildMessage(message);
        using var client = new SmtpClient { Timeout = (int)_options.Timeout.TotalMilliseconds };

        try
        {
            await client.ConnectAsync(_options.Host, _options.Port, _options.SecureSocketOption, cancellationToken);
            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Only the exception type is known here: its message may carry the server name or the login.
            return CampaignMailOutcome.NotAccepted;
        }

        try
        {
            await client.SendAsync(mime, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return CampaignMailOutcome.Unknown;
        }

        // The server accepted the message. A failure to say goodbye afterwards cannot change that, so it is not an outcome.
        // The goodbye runs without the caller's token: a shutdown must not turn an accepted message into a lost record.
        try
        {
            await client.DisconnectAsync(quit: true, CancellationToken.None);
        }
        catch (Exception)
        {
            // Ignored on purpose: the message is accepted, and the connection is disposed with the client.
        }

        return CampaignMailOutcome.Accepted;
    }

    /// <summary>Builds the message: a multipart/alternative body with the text and the HTML version of the same content.</summary>
    private MimeMessage BuildMessage(CampaignMailMessage message)
    {
        var body = new BodyBuilder
        {
            HtmlBody = message.HtmlBody,
            TextBody = message.PlainTextBody,
        };
        var mime = new MimeMessage
        {
            Subject = message.Subject,
            MessageId = message.MessageId,
            Body = body.ToMessageBody(),
        };
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.ToAddress));
        mime.Headers.Replace(UnsubscribeHeader, $"<{message.OneClickUnsubscribeUrl}>");
        mime.Headers.Replace(UnsubscribePostHeader, OneClickValue);
        mime.Headers.Replace(ContentLanguageHeader, message.Locale);
        return mime;
    }
}
