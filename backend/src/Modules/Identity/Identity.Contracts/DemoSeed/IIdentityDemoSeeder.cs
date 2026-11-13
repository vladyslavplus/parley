namespace Parley.Identity.Contracts.DemoSeed;

/// <summary>
/// Creates the demo organization, its teams, its accounts and their memberships. Every demo account gets the one password
/// passed in: existing accounts are reset to it, so a re-run keeps the documented sign-in. The host checks the environment
/// first and performs the refresh decision.
/// </summary>
public interface IIdentityDemoSeeder
{
    Task<IdentityDemoSeedResult> EnsureAsync(
        DemoOrganizationProfile organization,
        string accountPassword,
        CancellationToken cancellationToken);

    /// <summary>The instant of the last completed demo seed, or null when this organization was never seeded.</summary>
    Task<DateTime?> ReadSeededAtAsync(Guid organizationId, CancellationToken cancellationToken);

    /// <summary>Stamps a completed seed, so a fresh workspace is not generated again until the refresh window ends.</summary>
    Task MarkSeededAsync(Guid organizationId, DateTime seededAtUtc, CancellationToken cancellationToken);
}
