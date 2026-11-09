using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.DemoSeed;

namespace Parley.Api.Cli.SeedDemo;

/// <summary>The demo organization the seed creates or reuses. Values are fixed: the same workspace on every machine.</summary>
public static class SeedDemoOrganization
{
    public static Guid Id { get; } = new("0192f5a0-7b1c-7000-8000-00000000de30");

    public const string Name = "Parley Demo";

    private const string Slug = "parley-demo";

    private const string TimeZone = "Europe/Kyiv";

    public const string Locale = "uk";

    public const string Currency = CrmDemoContract.ExportCurrency;

    public static DemoOrganizationProfile Profile { get; } = new(Id, Name, Slug, TimeZone, Locale, Currency);
}
