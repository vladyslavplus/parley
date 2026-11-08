using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Outbox.Messages;

namespace Parley.SharedKernel.Outbox.Consumers;

/// <summary>
/// Consumer of one integration event type. Implementations are registered as
/// <c>IIntegrationEventConsumer{TEvent}</c> in the consuming module together
/// with the consumer's own DbContext through
/// <see cref="OutboxServiceCollectionExtensions.AddIntegrationEventConsumer"/>.
///
/// Delivery is at-least-once: the dispatcher runs one transaction on the
/// consumer's DbContext (inbox insert plus consumer work plus commit) and
/// marks the producer outbox row processed only after that commit. A crash
/// between the two redelivers the row, and the inbox primary key
/// (<c>Consumer</c>, <c>EventId</c>) turns the replay into a no-op. A failing
/// consumer rolls back its own writes and the outbox row is retried with
/// backoff. At-least-once delivery plus an idempotent inbox is effectively
/// once for consumers that only modify the tracked graph.
///
/// Implementations must therefore only modify the tracked graph and must NOT
/// call <c>SaveChanges</c> or <c>Commit</c> themselves; the dispatcher saves
/// atomically. Consumers perform database work only, never external I/O
/// (HTTP, queues, file storage), because external side effects cannot be
/// rolled back with the consumer transaction. A replay of an already-recorded
/// event is skipped entirely.
/// </summary>
public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IntegrationEvent
{
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
