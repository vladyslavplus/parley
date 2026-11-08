namespace Parley.SharedKernel.Outbox.Messages;

/// <summary>
/// A durable integration event row. One instance is inserted into the module
/// outbox table inside the same database transaction as the business change
/// that produced the event, so the change and the event commit together or
/// neither does. A rollback therefore publishes nothing.
///
/// The delivery dispatcher claims due rows with <c>FOR UPDATE SKIP LOCKED</c>,
/// delivers them to registered consumers and marks them processed. Rows are
/// never deleted by the dispatcher; processed rows are reaped by a periodic
/// cleanup job owned by the module.
///
/// Unlike <see cref="Parley.SharedKernel.Persistence.Entities.BusinessEntity"/> this row
/// deliberately carries no global query filter: the dispatcher is a
/// cross-organization sweep that enumerates due rows across organizations and
/// sets the tenant per row before delivery. Writers still fail closed: the
/// organization is always resolved from <c>ITenantContext</c> at write time,
/// so no row can be written without an organization.
/// </summary>
public sealed class OutboxMessage
{
    public OutboxMessage()
    {
    }

    public OutboxMessage(
        Guid organizationId,
        string type,
        string payload,
        DateTime occurredAt,
        DateTime nextAttemptAt)
    {
        OrganizationId = organizationId;
        Type = type;
        Payload = payload;
        OccurredAt = occurredAt;
        NextAttemptAt = nextAttemptAt;
    }

    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    /// <summary>
    /// The integration event type name (<c>Type.FullName</c> of the producer
    /// event). The dispatcher resolves the type from this name and
    /// deserializes <see cref="Payload"/> into it.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The serialized event as a JSON object (see <see cref="IntegrationEvent"/>
    /// for the envelope format).
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// When the business change committed, stamped from <c>TimeProvider</c>
    /// at write time.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// When the row stopped retrying as a dead letter. Dead letters keep
    /// <see cref="ProcessedAt"/> empty so a cleanup of processed rows never
    /// reaps them, and the dispatcher never claims them again.
    /// </summary>
    public DateTime? DeadLetteredAt { get; set; }

    public int Attempts { get; set; }

    /// <summary>
    /// The next due time for delivery. New rows are due immediately; failed
    /// deliveries move this to <c>now + exponential backoff</c>.
    /// </summary>
    public DateTime NextAttemptAt { get; set; }

    /// <summary>
    /// Short failure description (<c>ExceptionType: message</c>, truncated).
    /// It never contains the event payload or personal data.
    /// </summary>
    public string? LastError { get; set; }
}
