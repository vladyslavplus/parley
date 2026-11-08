using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>A contact was created. Carries identifiers only.</summary>
public sealed record ContactCreated : IntegrationEvent
{
    public Guid ContactId { get; init; }

    public Guid? OwnerUserId { get; init; }
}
