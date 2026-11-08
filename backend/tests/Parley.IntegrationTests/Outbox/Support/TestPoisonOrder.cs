using Parley.SharedKernel.Outbox.Messages;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// A test-only event whose same-context consumer runs a statement that fails
/// on the database, so the claim transaction is aborted outside SaveChanges.
/// </summary>
public sealed record TestPoisonOrder : IntegrationEvent
{
}
