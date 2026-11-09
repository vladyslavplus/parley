using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.Metrics;
using Parley.IntegrationTests.Crm.Metrics.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Metrics;

/// <summary>Keyset batches cover every contact once, in id order, whatever the batch size; out-of-contract sizes are refused.</summary>
public sealed class CustomerMetricsKeysetTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly Guid _organizationId = Guid.CreateVersion7();

    private CustomerMetricsFixtureData _data = null!;

    public async ValueTask InitializeAsync()
    {
        var token = TestContext.Current.CancellationToken;
        await postgres.ResetDataAsync(token);
        _data = CustomerMetricsFixtureLoader.Load();
        await CustomerMetricsFixtureSeed.SeedAsync(postgres.Factory.Services, _organizationId, _data, token);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(CrmCustomerMetricsContract.MaximumBatchSize)]
    public async Task BatchesCoverEveryContactOnceInIdOrder(int batchSize)
    {
        var rows = await CustomerMetricsReads.ReadAllAsync(
            postgres.Factory.Services,
            _organizationId,
            batchSize,
            _data.AsOf,
            TestContext.Current.CancellationToken);

        rows.Select(row => row.ContactId).Should().Equal(_data.ContactIds);
    }

    [Fact]
    public async Task RefusesBatchSizesOutsideTheContract()
    {
        using var scope = postgres.Factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var reader = scope.ServiceProvider.GetRequiredService<ICrmCustomerMetricsReader>();
        tenant.Set(_organizationId);
        var token = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => reader.ReadBatchAsync(null, 0, _data.AsOf, token));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            reader.ReadBatchAsync(null, CrmCustomerMetricsContract.MaximumBatchSize + 1, _data.AsOf, token));
    }
}
