namespace Parley.Campaigns.Features.Campaigns.Dispatch.Claims;

/// <summary>One claim as the insert statement reads it: the claim id, the contact, the recipient and the message id.</summary>
public sealed record SendInsertRow(Guid Id, Guid ContactId, Guid RecipientId, string MessageId);
