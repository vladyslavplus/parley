using Parley.Campaigns.Features.Campaigns.Shared;

namespace Parley.Campaigns.Features.Campaigns.List;

/// <summary>A campaign row with the creation instant, which the created sort key reads.</summary>
/// <param name="Summary">The row the list returns.</param>
/// <param name="CreatedAt">Creation instant, UTC.</param>
internal sealed record CampaignListRow(CampaignSummaryView Summary, DateTime CreatedAt);
