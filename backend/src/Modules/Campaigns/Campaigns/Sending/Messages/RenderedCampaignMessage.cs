namespace Parley.Campaigns.Sending.Messages;

/// <summary>The rendered subject (line breaks removed), the HTML body, and its plain-text alternative.</summary>
public sealed record RenderedCampaignMessage(string Subject, string HtmlBody, string PlainTextBody);
