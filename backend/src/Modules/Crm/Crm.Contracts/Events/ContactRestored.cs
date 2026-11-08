using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A soft-deleted contact was restored. Carries identifiers only.</summary>
public sealed record ContactRestored : IntegrationEvent
{
    public Guid ContactId { get; init; }

    public Guid? OwnerUserId { get; init; }
}
