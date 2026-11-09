using Parley.SharedKernel.Outbox.Messages;

namespace Parley.Crm.Contracts.Events;

/// <summary>
/// An order was recorded for a contact. Carries identifiers and amounts only. The demo seed loads orders in bulk
/// and publishes no events, so no code path emits this event yet.
/// </summary>
public sealed record OrderRecorded : IntegrationEvent
{
    public Guid OrderId { get; init; }

    public Guid ContactId { get; init; }

    public DateTimeOffset OrderedAt { get; init; }

    public decimal TotalAmount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsCancellation { get; init; }
}
