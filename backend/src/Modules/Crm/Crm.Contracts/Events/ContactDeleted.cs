using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A contact was soft deleted. Carries identifiers only.</summary>
public sealed record ContactDeleted : IntegrationEvent
{
    public Guid ContactId { get; init; }

    public Guid? OwnerUserId { get; init; }
}
