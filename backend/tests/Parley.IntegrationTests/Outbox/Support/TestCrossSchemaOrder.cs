using Parley.SharedKernel.Outbox.Messages;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// A test-only event consumed in a second schema by a different DbContext,
/// to prove cross-module consumer writes are not dropped.
/// </summary>
public sealed record TestCrossSchemaOrder : IntegrationEvent
{
    public Guid OrderId { get; init; }
}
