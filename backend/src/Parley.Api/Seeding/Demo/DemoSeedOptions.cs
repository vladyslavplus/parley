using Parley.Crm.Contracts.DemoSeed;

namespace Parley.Api.Seeding.Demo;

/// <summary>
/// Settings of the Development demo seed. Bind to <c>Demo:Seed</c>. The password is development data and has no default
/// in the committed configuration; the seed is off there and on only in the local Development example. The profile's
/// defaults are the production demo volumes.
/// </summary>
public sealed class DemoSeedOptions
{
    public const string SectionName = "Demo:Seed";

    public const int MinimumRefreshAfterDays = 1;

    public const int MaximumRefreshAfterDays = 365;

    public const int MinimumContacts = 1;

    public const int MaximumContacts = 20_000;

    public const int MinimumOrderMonths = 1;

    public const int MaximumOrderMonths = 120;

    /// <summary>Whether a Development host generates the demo workspace on start. Production ignores it.</summary>
    public bool Enabled { get; set; }

    /// <summary>A workspace older than this many days is wiped and generated again on the next start.</summary>
    public int RefreshAfterDays { get; set; } = 7;

    /// <summary>Shared password of every demo account; set it in the Development settings, never in the committed file.</summary>
    public string AccountPassword { get; set; } = string.Empty;

    /// <summary>Seed of every generated choice; the same seed and reference instant give the same rows.</summary>
    public long RandomSeed { get; set; } = 20261112;

    /// <summary>Volumes and shares of the generated workspace.</summary>
    public CrmDemoProfile Profile { get; set; } = new();
}
