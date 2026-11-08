using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Second-schema consumer that appends a tenant row in its own schema.
/// </summary>
public sealed class CrossSchemaOrderConsumer(SecondSchemaDbContext dbContext, DeliveryLedger ledger)
    : IIntegrationEventConsumer<TestCrossSchemaOrder>
{
    public Task ConsumeAsync(TestCrossSchemaOrder integrationEvent, CancellationToken cancellationToken)
    {
        dbContext.TenantRecords.Add(new TenantRecord(
            Guid.Empty,
            $"order-{integrationEvent.OrderId:D}"));
        ledger.Record(integrationEvent.OrderId);
        return Task.CompletedTask;
    }
}
