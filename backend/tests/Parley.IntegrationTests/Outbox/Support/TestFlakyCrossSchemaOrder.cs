using Parley.SharedKernel.Outbox.Messages;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// A test-only failing event for the second-schema consumer.
/// </summary>
public sealed record TestFlakyCrossSchemaOrder : IntegrationEvent
{
    public Guid OrderId { get; init; }
}
