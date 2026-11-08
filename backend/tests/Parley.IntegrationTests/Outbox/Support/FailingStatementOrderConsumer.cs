using Microsoft.EntityFrameworkCore;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Same-context consumer that runs a failing statement (division by zero).
/// PostgreSQL aborts the transaction, so only a savepoint can record the failure.
/// </summary>
public sealed class FailingStatementOrderConsumer(TenantTestDbContext dbContext) : IIntegrationEventConsumer<TestPoisonOrder>
{
    public async Task ConsumeAsync(TestPoisonOrder integrationEvent, CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync("SELECT 1 / 0", cancellationToken);
    }
}
