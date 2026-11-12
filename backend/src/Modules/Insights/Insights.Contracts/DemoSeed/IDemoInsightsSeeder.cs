namespace Parley.Insights.Contracts.DemoSeed;

/// <summary>
/// Builds the insights view of a freshly generated demo workspace: the RFM snapshot, the priority list, and (best effort)
/// the model sync and the uplift/CLV scoring runs, so the home page opens in ML mode when the service is reachable.
/// </summary>
public interface IDemoInsightsSeeder
{
    Task<DemoInsightsReport> SeedAsync(Guid organizationId, DateTimeOffset reference, CancellationToken cancellationToken);

    /// <summary>Removes every insights row of the organization, for a stale demo refresh.</summary>
    Task WipeAsync(Guid organizationId, CancellationToken cancellationToken);
}
