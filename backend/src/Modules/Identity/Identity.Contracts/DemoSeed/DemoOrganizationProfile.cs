namespace Parley.Identity.Contracts.DemoSeed;

/// <summary>Organization the demo seed creates when it is missing. Values are fixed by the host, not read from the export.</summary>
/// <param name="Id">Identifier of the organization.</param>
/// <param name="Name">Display name.</param>
/// <param name="Slug">Unique short identifier.</param>
/// <param name="TimeZone">IANA time zone of the organization.</param>
/// <param name="Locale">Default locale of the organization and of its members.</param>
/// <param name="Currency">ISO 4217 currency of the organization.</param>
public sealed record DemoOrganizationProfile(
    Guid Id,
    string Name,
    string Slug,
    string TimeZone,
    string Locale,
    string Currency);
