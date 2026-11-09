using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A deal was created or its owner changed. Carries identifiers only.</summary>
public sealed record DealAssigned : IntegrationEvent
{
    public Guid DealId { get; init; }

    public Guid OwnerUserId { get; init; }
}
