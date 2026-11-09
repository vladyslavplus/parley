namespace Parley.IntegrationTests.Crm.Metrics.Support;

/// <summary>The Python calculator's values for one contact, parsed from the fixture.</summary>
public sealed record ExpectedCustomerMetrics(
    Guid ContactId,
    DateTimeOffset? FirstOrderAt,
    DateTimeOffset? LastOrderAt,
    int OrderCount,
    int PurchaseDayCount,
    int Orders90Days,
    int Orders365Days,
    decimal TotalValue,
    decimal Value365Days,
    decimal AvgOrderValue,
    decimal AvgRepeatOrderValue,
    bool HasOpenDeal);
