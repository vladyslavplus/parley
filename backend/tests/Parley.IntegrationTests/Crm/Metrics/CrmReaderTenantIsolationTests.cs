using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.Audience;
using Parley.Crm.Contracts.Outcomes;
using Parley.IntegrationTests.Crm.Metrics.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Metrics;

/// <summary>Each CRM reader returns only the current organization's rows, even when asked for another organization's ids.</summary>
public sealed class CrmReaderTenantIsolationTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly Guid _organizationId = Guid.CreateVersion7();

    private readonly Guid _otherOrganizationId = Guid.CreateVersion7();

    private CustomerMetricsFixtureData _data = null!;

    private CustomerMetricsFixtureData _otherData = null!;

    public async ValueTask InitializeAsync()
    {
        var token = TestContext.Current.CancellationToken;
        await postgres.ResetDataAsync(token);
        _data = CustomerMetricsFixtureLoader.Load();
        await CustomerMetricsFixtureSeed.SeedAsync(postgres.Factory.Services, _organizationId, _data, token);

        var otherContact = Guid.CreateVersion7();
        _otherData = new CustomerMetricsFixtureData(
            _data.AsOf,
            [otherContact],
            [new FixtureOrder("other-1", otherContact, _data.AsOf.UtcDateTime.AddDays(-10), 42m, false)],
            [],
            []);
        await CustomerMetricsFixtureSeed.SeedAsync(postgres.Factory.Services, _otherOrganizationId, _otherData, token);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task MetricsAudienceAndOutcomeReadersSeeOnlyTheCurrentOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var otherContact = _otherData.ContactIds.Single();
        var ownContact = _data.ContactIds[0];

        var metrics = await CustomerMetricsReads.ReadAllAsync(postgres.Factory.Services, _organizationId, 50, _data.AsOf, token);
        var otherMetrics = await CustomerMetricsReads.ReadAllAsync(postgres.Factory.Services, _otherOrganizationId, 50, _data.AsOf, token);
        metrics.Select(row => row.ContactId).Should().Equal(_data.ContactIds);
        otherMetrics.Select(row => row.ContactId).Should().Equal([otherContact]);
        otherMetrics.Single().TotalValue.Should().Be(42m);

        using var scope = postgres.Factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        tenant.Set(_organizationId);
        var audience = await scope.ServiceProvider.GetRequiredService<ICrmAudienceReader>()
            .ReadEligibleBatchAsync(null, 1000, token);
        audience.Select(row => row.ContactId).Should().Equal(_data.ContactIds);
        audience.Should().NotContain(row => row.ContactId == otherContact);

        var outcomes = await scope.ServiceProvider.GetRequiredService<ICrmOutcomeReader>().ReadWindowAsync(
            [ownContact, otherContact],
            _data.AsOf.AddDays(-400),
            _data.AsOf,
            token);
        outcomes.Should().NotContain(row => row.ContactId == otherContact);
    }
}
