namespace Parley.Api.Controllers.Identity.Requests;

/// <summary>Body of <c>PUT /organizations/{id}</c>.</summary>
public sealed record UpdateOrganizationRequest(string? Name, string? TimeZone, string? DefaultLocale, string? Currency);
