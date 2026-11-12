namespace Parley.Identity.Contracts.Organizations;

/// <summary>An organization and its IANA time zone name.</summary>
/// <param name="OrganizationId">The organization.</param>
/// <param name="TimeZone">IANA time zone name, for example <c>Europe/Kyiv</c>.</param>
public sealed record OrganizationTimeZone(Guid OrganizationId, string TimeZone);
