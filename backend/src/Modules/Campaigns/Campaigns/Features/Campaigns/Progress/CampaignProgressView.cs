namespace Parley.Campaigns.Features.Campaigns.Progress;

/// <summary>
/// Send counts of a campaign. <c>Total</c> is the targeted treatment recipients. <c>Queued</c> is every recipient
/// without a final outcome yet (not claimed, claimed, or waiting for a retry), so the five counts add up to the total.
/// </summary>
public sealed record CampaignProgressView(int Total, int Queued, int Sent, int Failed, int Unknown, int Suppressed);
