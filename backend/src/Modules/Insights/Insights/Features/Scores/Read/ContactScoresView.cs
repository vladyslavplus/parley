namespace Parley.Insights.Features.Scores.Read;

/// <summary>The scores of a contact: its segments as chips, and the score entries the caller may see.</summary>
public sealed record ContactScoresView(
    Guid ContactId,
    string? RfmSegment,
    string? UpliftSegment,
    IReadOnlyList<ContactScoreItemView> Scores);
