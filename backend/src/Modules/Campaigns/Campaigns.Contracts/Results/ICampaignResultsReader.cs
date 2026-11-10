namespace Parley.Campaigns.Contracts.Results;

/// <summary>
/// Reads the stored result of a campaign for the organization of the current tenant. Null when the campaign does not
/// exist for that tenant, so another organization's campaign answers the same way as an unknown one. A campaign without
/// a computed result returns a snapshot with <see cref="CampaignResultsSnapshot.ComputedAt"/> null.
/// </summary>
public interface ICampaignResultsReader
{
    Task<CampaignResultsSnapshot?> FindAsync(Guid campaignId, CancellationToken cancellationToken);

    /// <summary>
    /// The lift summary of the most recently completed campaign that has a computed result, or null when none exists. Its
    /// verdict is decided here with the same rule as the results endpoint.
    /// </summary>
    Task<CampaignResultSummary?> FindLatestCompletedAsync(CancellationToken cancellationToken);
}
