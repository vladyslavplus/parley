namespace Parley.Api.Controllers.Campaigns.Requests;

/// <summary>Body of a template create or replace request. Missing fields fail validation.</summary>
/// <param name="Name">Template name.</param>
/// <param name="SubjectLiquid">Subject source.</param>
/// <param name="BodyLiquid">Body source.</param>
/// <param name="Locale">Culture code, <c>en</c> or <c>uk</c>.</param>
/// <param name="Version">Row version the client read; required on replace, ignored on create.</param>
public sealed record TemplateRequest(string? Name, string? SubjectLiquid, string? BodyLiquid, string? Locale, uint? Version);
