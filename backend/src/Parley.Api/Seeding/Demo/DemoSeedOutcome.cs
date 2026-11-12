using Parley.Campaigns.Contracts.DemoSeed;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.DemoSeed;
using Parley.Insights.Contracts.DemoSeed;

namespace Parley.Api.Seeding.Demo;

/// <summary>One attempt of the Development seed: whether it wrote anything, why it stopped, and the counts of each step.</summary>
public sealed record DemoSeedOutcome(
    string Status,
    TimeSpan Duration,
    IdentityDemoSeedResult? Identity = null,
    CrmDemoImportReport? Crm = null,
    CampaignDemoSeedReport? Campaigns = null,
    DemoInsightsReport? Insights = null)
{
    public const string Seeded = "seeded";

    public const string Regenerated = "regenerated";

    public const string Fresh = "fresh";

    public const string Disabled = "disabled";

    public const string NotDevelopment = "not_development";

    public const string MissingPassword = "missing_password";

    public const string Failed = "failed";
}
