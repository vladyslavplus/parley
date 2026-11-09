using AwesomeAssertions;
using Parley.Crm.Contracts.Metrics;
using Parley.IntegrationTests.Crm.Metrics.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Xunit;

namespace Parley.IntegrationTests.Crm.Metrics;

/// <summary>The CRM metrics reader gives the same values as the Python calculator on the committed fixture, in any batch size.</summary>
public sealed class CustomerMetricsReaderTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly decimal ToleranceForRoundedAverages = 0.0001m;

    // The hand-made edge contacts of the fixture: the generator's contact ids 101, 102 and 103.
    private static readonly Guid AtAsOfContactId = Guid.Parse("0192f5a0-7b1c-7000-8000-000000000065");

    private static readonly Guid AtShortWindowEdgeContactId = Guid.Parse("0192f5a0-7b1c-7000-8000-000000000066");

    private static readonly Guid AtLongWindowEdgeContactId = Guid.Parse("0192f5a0-7b1c-7000-8000-000000000067");

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

    [Fact]
    public async Task ReturnsThePythonValuesForEveryContact()
    {
        var rows = await CustomerMetricsReads.ReadAllAsync(
            postgres.Factory.Services,
            _organizationId,
            batchSize: 7,
            _data.AsOf,
            TestContext.Current.CancellationToken);

        rows.Select(row => row.ContactId).Should().Equal(_data.ContactIds);
        foreach (var (actual, expected) in rows.Zip(_data.Expected))
        {
            AssertMatches(actual, expected);
        }
    }

    /// <summary>
    /// One order exactly at as-of counts in both windows; one exactly at as-of minus the short window only in the long window;
    /// one exactly at as-of minus the long window in neither. The windows are (start, as-of], so a start edge is excluded.
    /// </summary>
    [Fact]
    public async Task WindowEdgesCountAsTheDefinitionsSay()
    {
        var rows = await CustomerMetricsReads.ReadAllAsync(
            postgres.Factory.Services,
            _organizationId,
            batchSize: 7,
            _data.AsOf,
            TestContext.Current.CancellationToken);

        var atAsOf = rows.Single(row => row.ContactId == AtAsOfContactId);
        atAsOf.Orders90Days.Should().Be(1);
        atAsOf.Orders365Days.Should().Be(1);
        atAsOf.TotalValue.Should().Be(10.00m);
        atAsOf.Value365Days.Should().Be(10.00m);

        var atShortEdge = rows.Single(row => row.ContactId == AtShortWindowEdgeContactId);
        atShortEdge.Orders90Days.Should().Be(0);
        atShortEdge.Orders365Days.Should().Be(1);
        atShortEdge.TotalValue.Should().Be(20.00m);
        atShortEdge.Value365Days.Should().Be(20.00m);

        var atLongEdge = rows.Single(row => row.ContactId == AtLongWindowEdgeContactId);
        atLongEdge.Orders90Days.Should().Be(0);
        atLongEdge.Orders365Days.Should().Be(0);
        atLongEdge.TotalValue.Should().Be(30.00m);
        atLongEdge.Value365Days.Should().Be(0.00m);
    }

    private static void AssertMatches(CrmCustomerMetricsRow actual, ExpectedCustomerMetrics expected)
    {
        var because = $"contact {expected.ContactId}";
        actual.ContactId.Should().Be(expected.ContactId, because);
        actual.FirstOrderAt.Should().Be(expected.FirstOrderAt, because);
        actual.LastOrderAt.Should().Be(expected.LastOrderAt, because);
        actual.OrderCount.Should().Be(expected.OrderCount, because);
        actual.PurchaseDayCount.Should().Be(expected.PurchaseDayCount, because);
        actual.Orders90Days.Should().Be(expected.Orders90Days, because);
        actual.Orders365Days.Should().Be(expected.Orders365Days, because);
        actual.TotalValue.Should().Be(expected.TotalValue, because);
        actual.Value365Days.Should().Be(expected.Value365Days, because);
        Math.Abs(actual.AvgOrderValue - expected.AvgOrderValue).Should().BeLessThanOrEqualTo(ToleranceForRoundedAverages, because);
        Math.Abs(actual.AvgRepeatOrderValue - expected.AvgRepeatOrderValue).Should().BeLessThanOrEqualTo(ToleranceForRoundedAverages, because);
        actual.HasOpenDeal.Should().Be(expected.HasOpenDeal, because);
    }
}
