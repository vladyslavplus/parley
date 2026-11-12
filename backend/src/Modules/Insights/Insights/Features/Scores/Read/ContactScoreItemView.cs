using System.Text.Json;

namespace Parley.Insights.Features.Scores.Read;

/// <summary>One score entry. Value and payload are null for a caller without the ML view.</summary>
public sealed record ContactScoreItemView(
    string Kind,
    string ModelName,
    string ModelVersion,
    string? Provenance,
    DateTime ScoredAt,
    decimal? Value,
    JsonElement? Payload);
