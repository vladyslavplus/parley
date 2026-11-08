namespace Parley.Identity.Features.Organizations.Get;

/// <summary>Settings of an organization as members and administrators read them.</summary>
public sealed record OrganizationView(Guid Id, string Name, string Slug, string TimeZone, string DefaultLocale, string Currency);
