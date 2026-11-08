using Parley.SharedKernel.Outbox.Messages;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// A test-only integration event pair proving cross-module delivery.
/// </summary>
public sealed record TestOrderPlaced : IntegrationEvent
{
    public Guid OrderId { get; init; }

    public string Note { get; init; } = string.Empty;
}
