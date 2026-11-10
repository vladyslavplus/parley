namespace Parley.Crm.Contracts.Dashboard;

/// <summary>
/// Aggregates for the dashboard of the current organization. Only totals leave this reader, never a record. Each method
/// takes the owner ids the caller may see: null means the whole organization, an empty collection means nothing.
/// </summary>
public interface ICrmDashboardReader
{
    /// <summary>Open deals grouped by stage, for the deals whose owner is in <paramref name="ownerUserIds"/>.</summary>
    Task<IReadOnlyList<CrmPipelineStageTotal>> ReadOpenPipelineAsync(
        IReadOnlyCollection<Guid>? ownerUserIds,
        CancellationToken cancellationToken);

    /// <summary>Open tasks due at or before <paramref name="dueAt"/>, for the tasks whose assignee is in <paramref name="assigneeUserIds"/>.</summary>
    Task<int> CountTasksDueAsync(
        DateTimeOffset dueAt,
        IReadOnlyCollection<Guid>? assigneeUserIds,
        CancellationToken cancellationToken);
}
