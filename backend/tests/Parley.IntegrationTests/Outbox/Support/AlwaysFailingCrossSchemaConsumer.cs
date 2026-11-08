using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Second-schema consumer that appends a row and then fails, to prove the
/// row and the inbox row roll back together.
/// </summary>
public sealed class AlwaysFailingCrossSchemaConsumer(SecondSchemaDbContext dbContext)
    : IIntegrationEventConsumer<TestFlakyCrossSchemaOrder>
{
    private const string FailureMessage = "second-schema boom";

    private int Attempts { get; set; }

    public Task ConsumeAsync(TestFlakyCrossSchemaOrder integrationEvent, CancellationToken cancellationToken)
    {
        Attempts++;
        dbContext.TenantRecords.Add(new TenantRecord(
            Guid.Empty,
            $"order-{integrationEvent.OrderId:D}"));
        throw new InvalidOperationException(FailureMessage);
    }
}
