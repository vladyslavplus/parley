using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>
/// A deal was created or its owner changed. Carries identifiers only. The actor is the user who made the change, so a
/// consumer can skip a self-assignment.
/// </summary>
public sealed record DealAssigned : IntegrationEvent
{
    public Guid DealId { get; init; }

    public Guid OwnerUserId { get; init; }

    public Guid ActorUserId { get; init; }
}
