namespace Parley.SharedKernel.Outbox.Messages;

/// <summary>
/// Base type for every cross-module integration event. Events are records
/// with a parameterless constructor so <c>System.Text.Json</c> can
/// deserialize them in the dispatcher.
///
/// Envelope format. An outbox row carries the event type name
/// (<c>Type.FullName</c>) in the <c>type</c> column and the event itself as a
/// flat JSON object in the <c>payload</c> column. The object contains every
/// public property of the derived record with its CLR property names
/// preserved (no camel-casing), so an event payload looks like:
/// <c>{"EventId":"...","OccurredAt":"...","Field1":...}</c>.
/// <c>EventId</c> is the message identity used as the inbox key; the
/// <c>occurred_at</c> outbox column mirrors <see cref="OccurredAt"/> and is
/// the authoritative ordering and due-time value.
/// </summary>
public abstract record IntegrationEvent
{
    protected IntegrationEvent()
    {
    }

    public Guid EventId { get; init; } = Guid.CreateVersion7();

    public DateTimeOffset OccurredAt { get; init; }
}
