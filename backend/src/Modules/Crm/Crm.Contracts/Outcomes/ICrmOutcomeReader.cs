namespace Parley.Crm.Contracts.Outcomes;

/// <summary>
/// Reads what the given contacts bought and won inside a window. The window is
/// <c>(windowStartExclusive, windowEndInclusive]</c>, both UTC. Contacts without any activity in the window
/// are not returned. Ids must be unique; the caller keeps batches small enough for one query.
/// <para>
/// Platform reader: it reads the orders and wins of the given contacts without the user data scopes, for platform
/// evaluation jobs. It must never back a user-facing endpoint; user-facing reads go through the scoped handlers.
/// </para>
/// </summary>
public interface ICrmOutcomeReader
{
    Task<IReadOnlyList<CrmOutcomeRow>> ReadWindowAsync(
        IReadOnlyList<Guid> contactIds,
        DateTimeOffset windowStartExclusive,
        DateTimeOffset windowEndInclusive,
        CancellationToken cancellationToken);
}
