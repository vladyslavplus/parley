namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>One customer of the export. Holds the source id and the country only.</summary>
/// <param name="CustomerRef">Source customer id, as written in the file.</param>
/// <param name="CountryIso2">ISO alpha-2 country, or null when the export has none.</param>
public sealed record DemoContactRecord(string CustomerRef, string? CountryIso2);
