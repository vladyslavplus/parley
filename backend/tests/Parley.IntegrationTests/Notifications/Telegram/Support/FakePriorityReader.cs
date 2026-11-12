using Parley.Insights.Contracts.Priority;

namespace Parley.IntegrationTests.Notifications.Telegram.Support;

/// <summary>The priority list of the tests: the entries the test put in, filtered and limited like the real reader.</summary>
public sealed class FakePriorityReader : IPriorityReader
{
    public List<PriorityEntry> Entries { get; } = [];

    public Task<IReadOnlyList<PriorityEntry>> ReadTopAsync(IReadOnlyCollection<Guid>? ownerUserIds, int limit, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PriorityEntry>>(
            Entries
                .Where(entry => ownerUserIds is null || (entry.OwnerUserId is { } owner && ownerUserIds.Contains(owner)))
                .Take(limit)
                .ToArray());
}
