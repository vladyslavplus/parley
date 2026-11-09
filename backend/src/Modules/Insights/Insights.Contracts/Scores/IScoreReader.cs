namespace Parley.Insights.Contracts.Scores;

/// <summary>
/// Read path of the latest scores of the current organization, used to rank campaign audiences. Scores come from the
/// scoring pipeline, so until it has produced one <see cref="HasScoresAsync"/> is false and a campaign that targets
/// by score cannot be prepared.
/// </summary>
public interface IScoreReader
{
    /// <summary>True when at least one score of the kind exists for the current organization.</summary>
    Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken);

    /// <summary>The latest score of each listed contact that has one. Contacts without a score are absent from the result.</summary>
    Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        CancellationToken cancellationToken);
}
