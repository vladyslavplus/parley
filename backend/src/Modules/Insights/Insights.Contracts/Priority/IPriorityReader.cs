namespace Parley.Insights.Contracts.Priority;

/// <summary>
/// Read path of the priority list of the current organization, from the latest completed build. It is the input of the
/// notification channels, so it applies no caller scope: a channel passes the owner it sends for.
/// </summary>
public interface IPriorityReader
{
    /// <summary>
    /// The best entries of the latest build. <paramref name="ownerUserIds"/> null means the whole organization; a collection
    /// limits the entries to contacts owned by one of its members, so the limit applies to what the caller may see.
    /// </summary>
    Task<IReadOnlyList<PriorityEntry>> ReadTopAsync(IReadOnlyCollection<Guid>? ownerUserIds, int limit, CancellationToken cancellationToken);
}
