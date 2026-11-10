namespace Parley.Crm.Contracts.Priority;

/// <summary>
/// Reads the priority facts of a batch of contacts in one statement: whether each has a due open task and a stale open
/// deal. Only live rows of the current organization count; a contact missing from the result has neither fact.
/// </summary>
public interface ICrmPriorityFactsReader
{
    /// <param name="contactIds">At most the CRM batch maximum of contacts.</param>
    /// <param name="dueAt">A task counts as due when its due instant is at or before this instant.</param>
    /// <param name="staleBefore">A deal counts as stale when it was last updated before this instant.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<IReadOnlyList<CrmPriorityFactsRow>> ReadBatchAsync(
        IReadOnlyCollection<Guid> contactIds,
        DateTimeOffset dueAt,
        DateTimeOffset staleBefore,
        CancellationToken cancellationToken);
}
