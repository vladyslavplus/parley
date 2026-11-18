namespace Parley.Insights.Features.Scores.Read;

/// <summary>
/// One score entry with the typed fields of its payload. The value, the payload fields and the currency are null for a
/// caller without the ML view. Uplift fills <c>PTreated</c> and <c>PControl</c>; CLV fills <c>PAlive</c>,
/// <c>ExpectedPurchases</c>, <c>ExpectedAverageValue</c> and <c>HorizonDays</c>, and carries the organization currency.
/// </summary>
public sealed record ContactScoreItemView(
    string Kind,
    string ModelName,
    string ModelVersion,
    string? Provenance,
    DateTime ScoredAt,
    decimal? Value,
    decimal? PTreated,
    decimal? PControl,
    decimal? PAlive,
    decimal? ExpectedPurchases,
    decimal? ExpectedAverageValue,
    int? HorizonDays,
    string? Currency);
