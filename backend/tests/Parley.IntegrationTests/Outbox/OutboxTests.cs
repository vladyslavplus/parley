using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Outbox.Support;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Exceptions;
using Parley.SharedKernel.Tenancy.Implementations;
using Xunit;

namespace Parley.IntegrationTests.Outbox;

/// <summary>
/// Proves transactional outbox/inbox delivery against a real Postgres
/// database: atomic publish, exactly-once delivery, rollback safety, parallel
/// dispatchers and retry with backoff.
/// </summary>
public sealed class OutboxTests(PostgresFixture fixture) : IAsyncLifetime
{
    private readonly PostgresFixture _fixture = fixture;

    public async ValueTask InitializeAsync() =>
        await _fixture.ResetDataAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task CommittedEvent_DeliveredExactlyOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var integrationEvent = NewOrderPlaced(Guid.CreateVersion7());
        var host = OutboxTestHost.Create(_fixture);

        await PublishAsync(host, organizationId,
            db => db.TenantRecords.Add(new TenantRecord(organizationId, "business-change")),
            writer => writer.Add(integrationEvent), token);

        var handled = await host.CreateDispatcher().ProcessDueMessagesAsync(token);

        handled.Should().Be(1, "one due message is handled by one pass");
        host.Ledger.Count.Should().Be(1, "the consumer applied the event once");
        var delivered = await ReadOutboxRowAsync(host, typeof(TestOrderPlaced).FullName, token);
        delivered.LastError.Should().BeNull("a delivered row records no failure");
        delivered.ProcessedAt.Should().NotBeNull("a delivered row is marked processed");
        (await CountInboxRowsAsync(host, integrationEvent.EventId, token))
            .Should().Be(1, "the consumer's inbox row commits with its work");

        // Replay: a second outbox row carrying the same event must still be
        // a no-op for the consumer (inbox).
        await InsertDuplicateRowAsync(host, organizationId, token);
        await host.CreateDispatcher().ProcessDueMessagesAsync(token);

        host.Ledger.Count.Should().Be(1, "replaying the same message must not double-apply");
    }

    [Fact]
    public async Task RolledBackTransaction_DeliversNothing()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var host = OutboxTestHost.Create(_fixture);
        host.TenantContext.Set(organizationId);

        using var strategyScope = host.CreateScope();
        var strategyDb = strategyScope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        var strategy = strategyDb.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var scope = host.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
            var writer = scope.ServiceProvider.GetRequiredService<IOutbox>();
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            db.TenantRecords.Add(new TenantRecord(organizationId, "business-change"));
            writer.Add(NewOrderPlaced(Guid.CreateVersion7()));
            await db.SaveChangesAsync(token);
            await transaction.RollbackAsync(token);
        });

        var handled = await host.CreateDispatcher().ProcessDueMessagesAsync(token);

        handled.Should().Be(0, "a rolled-back transaction publishes nothing");
        host.Ledger.Count.Should().Be(0);
    }

    [Fact]
    public async Task TwoDispatchers_NeverProcessOneRowTwice()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var host = OutboxTestHost.Create(_fixture);
        var orderIds = new List<Guid>();
        for (var index = 0; index < 6; index++)
        {
            var orderId = Guid.CreateVersion7();
            orderIds.Add(orderId);
            var integrationEvent = NewOrderPlaced(orderId);
            await PublishAsync(host, organizationId, _ => { }, writer => writer.Add(integrationEvent), token);
        }

        var first = host.CreateDispatcher();
        var second = host.CreateDispatcher();
        await Task.WhenAll(
            first.ProcessDueMessagesAsync(token),
            second.ProcessDueMessagesAsync(token));

        host.Ledger.Count.Should().Be(6, "two dispatchers must deliver every row exactly once");
        foreach (var orderId in orderIds)
        {
            host.Ledger.DeliveredOrderIds.Count(delivered => delivered == orderId).Should().Be(1);
        }
    }

    [Fact]
    public async Task FailingConsumer_RetriedWithBackoffAndStopsAtCap()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var baseBackoff = TimeSpan.FromSeconds(10);
        var host = OutboxTestHost.Create(_fixture, new OutboxOptions
        {
            PollInterval = TimeSpan.FromSeconds(1),
            BatchSize = 10,
            MaxAttempts = 3,
            BaseBackoff = baseBackoff,
        }, registerFailingConsumer: true);

        var integrationEvent = new TestFlakyOrder
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = _fixture.TimeProvider.GetUtcNow(),
            OrderId = Guid.CreateVersion7(),
        };
        await PublishAsync(host, organizationId, _ => { }, writer => writer.Add(integrationEvent), token);

        var beforeFirstAttempt = _fixture.TimeProvider.GetUtcNow().UtcDateTime;

        await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        var afterFirst = await ReadOutboxRowAsync(host, typeof(TestFlakyOrder).FullName, token);

        afterFirst.Attempts.Should().Be(1);
        afterFirst.ProcessedAt.Should().BeNull("a failed delivery is not processed");
        afterFirst.NextAttemptAt.Should().BeAfter(beforeFirstAttempt);
        afterFirst.NextAttemptAt.Should().BeOnOrBefore(beforeFirstAttempt.Add(baseBackoff).Add(baseBackoff / 4));
        afterFirst.LastError.Should().Contain("boom");

        await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        (await ReadOutboxRowAsync(host, typeof(TestFlakyOrder).FullName, token)).Attempts.Should().Be(1,
            "an immediate pass must not retry before the backoff window passes");

        _fixture.TimeProvider.Advance(TimeSpan.FromSeconds(20));
        await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        (await ReadOutboxRowAsync(host, typeof(TestFlakyOrder).FullName, token)).Attempts.Should().Be(2);

        _fixture.TimeProvider.Advance(TimeSpan.FromSeconds(60));
        await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        var capped = await ReadOutboxRowAsync(host, typeof(TestFlakyOrder).FullName, token);

        capped.Attempts.Should().Be(3);
        capped.ProcessedAt.Should().BeNull("a dead letter stays unprocessed so processed-row cleanup never reaps it");
        capped.DeadLetteredAt.Should().NotBeNull("at the max-attempts cap the row stops retrying as a dead letter");
        host.FailingConsumer.Attempts.Should().Be(3, "the consumer ran once per attempt");

        var handled = await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        handled.Should().Be(0, "a capped row is not due anymore");
    }

    [Fact]
    public async Task CrossModuleConsumer_CommitsInboxAndWorkTogetherInConsumerSchema()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var orderId = Guid.CreateVersion7();
        var integrationEvent = new TestCrossSchemaOrder
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = _fixture.TimeProvider.GetUtcNow(),
            OrderId = orderId,
        };
        var host = OutboxTestHost.Create(_fixture, registerSecondSchemaConsumer: true);

        await PublishAsync(host, organizationId, _ => { }, writer => writer.Add(integrationEvent), token);

        var handled = await host.CreateDispatcher().ProcessDueMessagesAsync(token);

        handled.Should().Be(1);
        host.Ledger.Count.Should().Be(1, "the second-schema consumer applied the event");
        (await CountSecondSchemaTenantRowsAsync(host, organizationId, orderId, token)).Should().Be(1,
            "consumer work in the consumer schema must be visible, not silently dropped");
        (await CountSecondSchemaInboxRowsAsync(host, integrationEvent.EventId, token)).Should().Be(1,
            "the inbox row commits in the consumer schema with the work");
        (await ReadOutboxRowAsync(host, typeof(TestCrossSchemaOrder).FullName, token)).ProcessedAt
            .Should().NotBeNull("the producer row is marked processed after the consumer commit");

        // Replay: a duplicate producer row carrying the same event must still
        // be a no-op for the second-schema consumer.
        await InsertDuplicateRowAsync(host, organizationId, token);
        await host.CreateDispatcher().ProcessDueMessagesAsync(token);

        host.Ledger.Count.Should().Be(1, "replaying the same message must not double-apply");
        (await CountSecondSchemaTenantRowsAsync(host, organizationId, orderId, token)).Should().Be(1);
        (await CountSecondSchemaInboxRowsAsync(host, integrationEvent.EventId, token)).Should().Be(1);
    }

    [Fact]
    public async Task CrossModuleThrowingConsumer_LeavesNeitherDataNorInbox()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var integrationEvent = new TestFlakyCrossSchemaOrder
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = _fixture.TimeProvider.GetUtcNow(),
            OrderId = Guid.CreateVersion7(),
        };
        var host = OutboxTestHost.Create(_fixture, new OutboxOptions
        {
            PollInterval = TimeSpan.FromSeconds(1),
            BatchSize = 10,
            MaxAttempts = 3,
            BaseBackoff = TimeSpan.FromSeconds(5),
        }, registerSecondSchemaConsumer: true);

        await PublishAsync(host, organizationId, _ => { }, writer => writer.Add(integrationEvent), token);

        await host.CreateDispatcher().ProcessDueMessagesAsync(token);

        (await CountSecondSchemaTenantRowsAsync(host, organizationId, integrationEvent.OrderId, token)).Should().Be(0,
            "a throwing consumer rolls back its own writes");
        (await CountSecondSchemaInboxRowsAsync(host, integrationEvent.EventId, token)).Should().Be(0,
            "a throwing consumer leaves no inbox row");
        var row = await ReadOutboxRowAsync(host, typeof(TestFlakyCrossSchemaOrder).FullName, token);
        row.Attempts.Should().Be(1);
        row.ProcessedAt.Should().BeNull("a failed delivery is not processed");
        row.DeadLetteredAt.Should().BeNull("below the cap the row is retried, not dead-lettered");
    }

    [Fact]
    public async Task OutboxRows_CarryOrganizationId()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var host = OutboxTestHost.Create(_fixture);

        await PublishAsync(host, organizationId, _ => { }, writer => writer.Add(NewOrderPlaced(Guid.CreateVersion7())), token);

        var row = await ReadOutboxRowAsync(host, typeof(TestOrderPlaced).FullName, token);
        row.OrganizationId.Should().Be(organizationId);
        row.Type.Should().Be(typeof(TestOrderPlaced).FullName);
        row.Payload.Should().Contain("OrderId");
        row.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);

        // Fail-closed: writing without a tenant throws instead of writing an
        // empty organization.
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        var writer = new DbContextOutbox<TenantTestDbContext>(db, new JobTenantContext(), _fixture.TimeProvider);
        Action add = () => writer.Add(NewOrderPlaced(Guid.CreateVersion7()));
        add.Should().Throw<TenantContextNotSetException>("writing an outbox row without a tenant must fail closed");
    }

    [Fact]
    public async Task PoisonMessage_DeadLettersWithoutBlockingHealthyMessages()
    {
        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        var healthyOrderId = Guid.CreateVersion7();
        const int maxAttempts = 3;
        var host = OutboxTestHost.Create(_fixture, new OutboxOptions
        {
            PollInterval = TimeSpan.FromSeconds(1),
            BatchSize = 10,
            MaxAttempts = maxAttempts,
            BaseBackoff = TimeSpan.FromSeconds(5),
        }, registerPoisonConsumer: true);

        // Older than the healthy event, so the poison row is claimed first.
        var poison = new TestPoisonOrder
        {
            EventId = Guid.CreateVersion7(),
            OccurredAt = _fixture.TimeProvider.GetUtcNow().AddMinutes(-1),
        };
        await PublishAsync(host, organizationId, _ => { },
            writer =>
            {
                writer.Add(poison);
                writer.Add(NewOrderPlaced(healthyOrderId));
            }, token);

        // Pass 1: the poison row fails inside its claim transaction; a healthy
        // row in the same batch must still be delivered.
        await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        host.Ledger.DeliveredOrderIds.Should().Contain(healthyOrderId,
            "a poison row never blocks a healthy message in the same pass");

        for (var pass = 2; pass <= maxAttempts; pass++)
        {
            _fixture.TimeProvider.Advance(TimeSpan.FromHours(1));
            await host.CreateDispatcher().ProcessDueMessagesAsync(token);
        }

        var row = await ReadOutboxRowAsync(host, typeof(TestPoisonOrder).FullName, token);
        row.Attempts.Should().Be(maxAttempts, "each pass records one failed attempt, so the cap is reached");
        row.DeadLetteredAt.Should().NotBeNull("the row stops retrying at the attempt cap");
        row.ProcessedAt.Should().BeNull("a dead letter is never marked processed");
        row.LastError.Should().NotBeNullOrWhiteSpace("the last failure is preserved on the row");
        host.Ledger.DeliveredOrderIds.Count(id => id == healthyOrderId).Should().Be(1,
            "the healthy message is delivered exactly once");
    }

    [Fact]
    public async Task ScopeFailure_PassReturnsInsteadOfThrowing()
    {
        var token = TestContext.Current.CancellationToken;
        var host = OutboxTestHost.Create(_fixture);
        var dispatcher = new OutboxDispatcher<TenantTestDbContext>(
            new ThrowingScopeFactory(),
            Options.Create(new OutboxOptions()),
            host.TenantContext,
            _fixture.TimeProvider,
            NullLogger<OutboxDispatcher<TenantTestDbContext>>.Instance,
            []);

        var handled = await dispatcher.ProcessDueMessagesAsync(token);

        handled.Should().Be(0, "a failing pass is logged and never escapes the dispatcher loop, so the host keeps running");
    }

    private sealed class ThrowingScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => throw new InvalidOperationException("Scope factory is unavailable.");
    }

    private TestOrderPlaced NewOrderPlaced(Guid orderId) => new()
    {
        EventId = Guid.CreateVersion7(),
        OccurredAt = _fixture.TimeProvider.GetUtcNow(),
        OrderId = orderId,
        Note = "test order",
    };

    private async Task PublishAsync(
        OutboxTestHost host,
        Guid organizationId,
        Action<TenantTestDbContext> writeBusinessChange,
        Action<IOutbox> publish,
        CancellationToken token)
    {
        host.TenantContext.Set(organizationId);
        using var strategyScope = host.CreateScope();
        var strategyDb = strategyScope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        var strategy = strategyDb.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var scope = host.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
            var writer = scope.ServiceProvider.GetRequiredService<IOutbox>();
            await using var transaction = await db.Database.BeginTransactionAsync(token);
            writeBusinessChange(db);
            publish(writer);
            await db.SaveChangesAsync(token);
            await transaction.CommitAsync(token);
        });
    }

    private async Task InsertDuplicateRowAsync(
        OutboxTestHost host,
        Guid organizationId,
        CancellationToken token)
    {
        host.TenantContext.Set(organizationId);
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        var source = await db.Set<OutboxMessage>()
            .SingleAsync(entry => entry.OrganizationId == organizationId, token);
        var duplicate = new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = source.OrganizationId,
            Type = source.Type,
            Payload = source.Payload,
            OccurredAt = source.OccurredAt,
            NextAttemptAt = source.NextAttemptAt,
        };
        db.Set<OutboxMessage>().Add(duplicate);
        await db.SaveChangesAsync(token);
    }

    private async Task<OutboxMessage> ReadOutboxRowAsync(
        OutboxTestHost host,
        string? typeName,
        CancellationToken token)
    {
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        return await db.Set<OutboxMessage>().SingleAsync(entry => entry.Type == typeName, token);
    }

    private async Task<int> CountInboxRowsAsync(
        OutboxTestHost host,
        Guid eventId,
        CancellationToken token)
    {
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TenantTestDbContext>();
        return await db.Set<InboxMessage>().CountAsync(entry => entry.EventId == eventId, token);
    }

    private async Task<int> CountSecondSchemaTenantRowsAsync(
        OutboxTestHost host,
        Guid organizationId,
        Guid orderId,
        CancellationToken token)
    {
        host.TenantContext.Set(organizationId);
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecondSchemaDbContext>();
        return await db.TenantRecords.CountAsync(entry => entry.Name == $"order-{orderId:D}", token);
    }

    private async Task<int> CountSecondSchemaInboxRowsAsync(
        OutboxTestHost host,
        Guid eventId,
        CancellationToken token)
    {
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecondSchemaDbContext>();
        return await db.Set<InboxMessage>().CountAsync(entry => entry.EventId == eventId, token);
    }
}
