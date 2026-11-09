namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>A contact that passed the segment and the suppression filters, with its arm and bucket.</summary>
public sealed record CampaignFreezeCandidate(Guid ContactId, string? Email, string Arm, int Bucket);
