namespace Parley.IntegrationTests.Crm.Metrics.Support;

/// <summary>The committed metrics fixture: inputs, the open-deal contacts and the Python expectations.</summary>
/// <param name="AsOf">As-of instant of the expectations.</param>
/// <param name="ContactIds">Contacts in ascending id order.</param>
/// <param name="Orders">Invoices, including some after the as-of instant.</param>
/// <param name="OpenDealContactIds">Contacts with a live open deal.</param>
/// <param name="Expected">Expected metrics per contact, same order as <paramref name="ContactIds"/>.</param>
public sealed record CustomerMetricsFixtureData(
    DateTimeOffset AsOf,
    IReadOnlyList<Guid> ContactIds,
    IReadOnlyList<FixtureOrder> Orders,
    IReadOnlyList<Guid> OpenDealContactIds,
    IReadOnlyList<ExpectedCustomerMetrics> Expected);
