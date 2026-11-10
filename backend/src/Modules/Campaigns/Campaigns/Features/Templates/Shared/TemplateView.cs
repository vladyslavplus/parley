namespace Parley.Campaigns.Features.Templates.Shared;

/// <summary>A template with its subject and body source.</summary>
/// <param name="Id">Template identifier.</param>
/// <param name="Name">Name shown in the template library.</param>
/// <param name="SubjectLiquid">Subject source.</param>
/// <param name="BodyLiquid">Body source.</param>
/// <param name="Locale">Culture code the template is written in.</param>
/// <param name="Version">Row version to send back on update; a changed row answers 409.</param>
public sealed record TemplateView(Guid Id, string Name, string SubjectLiquid, string BodyLiquid, string Locale, uint Version);
