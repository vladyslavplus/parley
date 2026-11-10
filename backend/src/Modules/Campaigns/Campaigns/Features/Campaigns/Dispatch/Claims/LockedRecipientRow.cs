namespace Parley.Campaigns.Features.Campaigns.Dispatch.Claims;

/// <summary>Unmapped row of the recipient lock statement.</summary>
public sealed class LockedRecipientRow
{
    public Guid RecipientId { get; set; }

    public Guid ContactId { get; set; }
}
