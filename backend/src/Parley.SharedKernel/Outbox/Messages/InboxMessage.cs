namespace Parley.SharedKernel.Outbox.Messages;

/// <summary>
/// The idempotency record of one consumed event. The primary key is
/// (<c>Consumer</c>, <c>EventId</c>): the consumer's work and this row are
/// committed in the same transaction, so re-delivering the same event is a
/// no-op instead of a double-apply.
/// </summary>
public sealed class InboxMessage
{
    public InboxMessage()
    {
    }

    public InboxMessage(string consumer, Guid eventId, DateTime receivedAt)
    {
        Consumer = consumer;
        EventId = eventId;
        ReceivedAt = receivedAt;
    }

    /// <summary>
    /// Full type name of the consumer that applied the event.
    /// </summary>
    public string Consumer { get; set; } = string.Empty;

    /// <summary>
    /// The <see cref="IntegrationEvent.EventId"/> of the applied event
    /// (the message identity, distinct from the outbox row identifier).
    /// </summary>
    public Guid EventId { get; set; }

    public DateTime ReceivedAt { get; set; }
}
