namespace Parley.Campaigns.Features.Templates.Shared;

/// <summary>A template row of the library list.</summary>
/// <param name="Id">Template identifier.</param>
/// <param name="Name">Template name.</param>
/// <param name="Locale">Culture code the template is written in.</param>
public sealed record TemplateSummaryView(Guid Id, string Name, string Locale);
