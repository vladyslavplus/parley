using System.Collections.Frozen;

namespace Parley.Insights.Contracts.Scores;

/// <summary>
/// Reader used until the scoring pipeline is available: no kind has any score. Campaigns that rank by score fail to
/// prepare with a coded error instead of silently targeting nobody.
/// </summary>
public sealed class NoScoreReader : IScoreReader
{
    public Task<bool> HasScoresAsync(string scoreKind, CancellationToken cancellationToken) => Task.FromResult(false);

    public Task<IReadOnlyDictionary<Guid, ContactScore>> ReadLatestScoresAsync(
        string scoreKind,
        IReadOnlyList<Guid> contactIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, ContactScore>>(FrozenDictionary<Guid, ContactScore>.Empty);
}
