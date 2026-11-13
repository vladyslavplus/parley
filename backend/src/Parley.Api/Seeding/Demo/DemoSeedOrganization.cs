using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.DemoSeed;
using Parley.SharedKernel.Localization;

namespace Parley.Api.Seeding.Demo;

/// <summary>The demo organization the Development seed creates or reuses. Values are fixed: the same workspace on every machine.</summary>
public static class DemoSeedOrganization
{
    public static Guid Id { get; } = new("0192f5a0-7b1c-7000-8000-00000000de30");

    public const string Name = "Parley Demo";

    private const string Slug = "parley-demo";

    private const string TimeZone = "Europe/Kyiv";

    public const string Locale = CultureCodes.Ukrainian;

    public const string Currency = CrmDemoContract.DemoCurrency;

    public static DemoOrganizationProfile Profile { get; } = new(Id, Name, Slug, TimeZone, Locale, Currency);
}
