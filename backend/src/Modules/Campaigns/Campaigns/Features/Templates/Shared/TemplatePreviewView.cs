namespace Parley.Campaigns.Features.Templates.Shared;

/// <summary>A template rendered with sample recipient data. The body is HTML-escaped; the subject is plain text.</summary>
/// <param name="Subject">Rendered subject.</param>
/// <param name="Body">Rendered body.</param>
public sealed record TemplatePreviewView(string Subject, string Body);
