namespace Parley.IntegrationTests.Crm.Metrics.Support;

/// <summary>One invoice of the metrics fixture.</summary>
public sealed record FixtureOrder(
    string OrderId,
    Guid ContactId,
    DateTime OrderedAt,
    decimal TotalAmount,
    bool IsCancellation);
