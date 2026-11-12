namespace Parley.Campaigns.Sending.Unsubscribe;

/// <summary>What a signed unsubscribe token names: the organization, the campaign and the contact it was issued for.</summary>
/// <param name="OrganizationId">Organization of the recipient. The unsubscribe request runs under it.</param>
/// <param name="CampaignId">Campaign whose email carried the link.</param>
/// <param name="ContactId">Contact the link was issued to.</param>
/// <param name="ExpiresAt">Instant after which the link no longer works.</param>
public sealed record UnsubscribeTokenClaims(Guid OrganizationId, Guid CampaignId, Guid ContactId, DateTimeOffset ExpiresAt);
