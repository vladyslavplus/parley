using System.Collections.Concurrent;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Thread-safe delivery ledger shared by the test consumers.
/// </summary>
public sealed class DeliveryLedger
{
    private readonly ConcurrentBag<Guid> _deliveredOrderIds = new();

    public IReadOnlyCollection<Guid> DeliveredOrderIds => _deliveredOrderIds;

    public int Count => _deliveredOrderIds.Count;

    public void Record(Guid orderId) => _deliveredOrderIds.Add(orderId);
}
