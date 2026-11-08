using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Consumer that applies the event by appending a tenant row (the business
/// side effect) and records it in the ledger. Like a handler, it only
/// modifies the tracked graph; the dispatcher saves atomically. The row's
/// organization is left empty on purpose: the audit interceptor stamps the
/// dispatcher-provided per-row tenant.
/// </summary>
public sealed class RecordingOrderConsumer(TenantTestDbContext dbContext, DeliveryLedger ledger)
    : IIntegrationEventConsumer<TestOrderPlaced>
{
    public Task ConsumeAsync(TestOrderPlaced integrationEvent, CancellationToken cancellationToken)
    {
        dbContext.TenantRecords.Add(new TenantRecord(
            Guid.Empty,
            $"order-{integrationEvent.OrderId:D}"));
        ledger.Record(integrationEvent.OrderId);
        return Task.CompletedTask;
    }
}
