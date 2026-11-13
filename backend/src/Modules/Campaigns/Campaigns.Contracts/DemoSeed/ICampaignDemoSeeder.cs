namespace Parley.Campaigns.Contracts.DemoSeed;

/// <summary>
/// Creates the historic demo campaigns of the demo seed (domain model 11) through the production freeze, send and
/// attribution jobs, with source <c>demo_seed</c>, plus the draft and the prepared scheduled campaign. Only the
/// Development seed calls it, after the CRM step. A re-run finds the same campaigns and writes no new simulated orders.
/// </summary>
public interface ICampaignDemoSeeder
{
    /// <summary>Creates or resumes both campaigns and returns their results with the injected-effect figures.</summary>
    /// <param name="organizationId">The demo organization.</param>
    /// <param name="datasetEnd">Last order instant of the generated workspace; the historic campaigns are dated back from it.</param>
    /// <param name="cancellationToken">Cancels the run; a re-run resumes it.</param>
    Task<CampaignDemoSeedReport> SeedAsync(Guid organizationId, DateTimeOffset datasetEnd, CancellationToken cancellationToken);

    /// <summary>Removes every campaign, template, segment and suppression of the organization, for a stale demo refresh.</summary>
    Task WipeAsync(Guid organizationId, CancellationToken cancellationToken);
}
