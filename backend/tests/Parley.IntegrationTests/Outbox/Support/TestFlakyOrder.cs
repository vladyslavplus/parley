using Parley.SharedKernel.Outbox.Messages;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// A test-only event whose consumer always fails, to prove retry and caps.
/// </summary>
public sealed record TestFlakyOrder : IntegrationEvent
{
    public Guid OrderId { get; init; }
}
