namespace Parley.Campaigns.Sending.Mail;

/// <summary>One rendered campaign email, ready for the sender. Every value is final: the sender adds headers and the MIME structure only.</summary>
/// <param name="MessageId">Deterministic message id, without angle brackets.</param>
/// <param name="ToAddress">Recipient address.</param>
/// <param name="Subject">Subject with carriage returns and line feeds already removed.</param>
/// <param name="HtmlBody">Rendered HTML body with every value encoded.</param>
/// <param name="PlainTextBody">Text alternative of the same message, generated from the HTML. Keeps the unsubscribe link.</param>
/// <param name="OneClickUnsubscribeUrl">API POST link, carried in the List-Unsubscribe header (RFC 8058).</param>
/// <param name="Locale">Culture of the recipient, sent as Content-Language.</param>
public sealed record CampaignMailMessage(
    string MessageId,
    string ToAddress,
    string Subject,
    string HtmlBody,
    string PlainTextBody,
    string OneClickUnsubscribeUrl,
    string Locale);
