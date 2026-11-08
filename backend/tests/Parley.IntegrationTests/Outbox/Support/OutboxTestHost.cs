using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Outbox.Dispatching;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Implementations;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Test-only composition root for the outbox dispatcher, mirroring production
/// registrations (tenancy from <c>AddTenancy</c>, typed options, scoped
/// DbContexts, in-process consumers with their DbContext registrations).
/// Every helper below works on a fresh scope, so parallel dispatchers never
/// share a <c>DbContext</c>.
/// </summary>
public sealed class OutboxTestHost
{
    private readonly ServiceProvider _provider;

    private OutboxTestHost(
        ServiceProvider provider,
        DeliveryLedger ledger,
        AlwaysFailingOrderConsumer failingConsumer)
    {
        _provider = provider;
        Ledger = ledger;
        FailingConsumer = failingConsumer;
    }

    public DeliveryLedger Ledger { get; }

    public AlwaysFailingOrderConsumer FailingConsumer { get; }

    public static OutboxTestHost Create(
        PostgresFixture fixture,
        OutboxOptions? options = null,
        bool registerFailingConsumer = false,
        bool registerSecondSchemaConsumer = false,
        bool registerPoisonConsumer = false)
    {
        var ledger = new DeliveryLedger();
        var failingConsumer = new AlwaysFailingOrderConsumer();

        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(fixture.TimeProvider);
        services.AddHttpContextAccessor();
        services.AddTenancy();
        services.AddSingleton(ledger);

        var configuredOptions = options ?? new OutboxOptions();
        services.AddOptions<OutboxOptions>().Configure(o =>
        {
            o.PollInterval = configuredOptions.PollInterval;
            o.BatchSize = configuredOptions.BatchSize;
            o.MaxAttempts = configuredOptions.MaxAttempts;
            o.BaseBackoff = configuredOptions.BaseBackoff;
        });

        services.AddIntegrationEventConsumer<TestOrderPlaced, TenantTestDbContext, RecordingOrderConsumer>();
        if (registerPoisonConsumer)
        {
            services.AddIntegrationEventConsumer<TestPoisonOrder, TenantTestDbContext, FailingStatementOrderConsumer>();
        }
        if (registerFailingConsumer)
        {
            services.AddSingleton(failingConsumer);
            services.AddSingleton<IIntegrationEventConsumer<TestFlakyOrder>>(failingConsumer);
            services.AddSingleton(new ConsumerDbContextRegistration
            {
                ConsumerType = typeof(AlwaysFailingOrderConsumer),
                DbContextType = typeof(TenantTestDbContext),
            });
        }

        services.AddScoped(provider =>
        {
            var tenant = provider.GetRequiredService<ITenantContext>();
            return new TenantTestDbContext(
                TenantTestDbContextFactory.CreateOptions(fixture.ConnectionString, tenant, fixture.TimeProvider),
                tenant);
        });
        services.AddScoped<IOutbox>(provider => new DbContextOutbox<TenantTestDbContext>(
            provider.GetRequiredService<TenantTestDbContext>(),
            provider.GetRequiredService<ITenantContext>(),
            provider.GetRequiredService<TimeProvider>()));

        if (registerSecondSchemaConsumer)
        {
            services.AddScoped(provider =>
            {
                var tenant = provider.GetRequiredService<ITenantContext>();
                return new SecondSchemaDbContext(
                    SecondSchemaDbContextFactory.CreateOptions(fixture.ConnectionString, tenant, fixture.TimeProvider),
                    tenant);
            });
            services.AddIntegrationEventConsumer<TestCrossSchemaOrder, SecondSchemaDbContext, CrossSchemaOrderConsumer>();
            services.AddIntegrationEventConsumer<TestFlakyCrossSchemaOrder, SecondSchemaDbContext, AlwaysFailingCrossSchemaConsumer>();
        }

        return new OutboxTestHost(services.BuildServiceProvider(), ledger, failingConsumer);
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public IMutableTenantContext TenantContext =>
        _provider.GetRequiredService<IMutableTenantContext>();

    public OutboxDispatcher<TenantTestDbContext> CreateDispatcher() => new(
        _provider.GetRequiredService<IServiceScopeFactory>(),
        _provider.GetRequiredService<IOptions<OutboxOptions>>(),
        TenantContext,
        _provider.GetRequiredService<TimeProvider>(),
        NullLogger<OutboxDispatcher<TenantTestDbContext>>.Instance,
        _provider.GetServices<ConsumerDbContextRegistration>());
}
