using Parley.SharedKernel.Outbox.Messages;

namespace Parley.SharedKernel.Outbox.Publishing;

/// <summary>
/// Writes integration events into the module outbox table. The message is
/// added to the current <c>DbContext</c>, so the event and the business
/// change commit in ONE transaction: a caller controls atomicity simply by
/// saving both through the same context. No external I/O happens here; the
/// dispatcher delivers to consumers after the commit.
/// </summary>
public interface IOutbox
{
    void Add<TEvent>(TEvent integrationEvent) where TEvent : IntegrationEvent;
}
