namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>One recipient row as the freeze inserts it. Serialized to JSON and read back by the insert statement.</summary>
public sealed record RecipientInsertRow(
    Guid Id,
    Guid ContactId,
    string Arm,
    int AssignmentBucket,
    string? ScoreKind,
    decimal? ScoreValue,
    string? ModelVersion,
    int? RankInAudience,
    bool IsTargeted,
    string? EmailSnapshot,
    string Locale);
