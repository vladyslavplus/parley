namespace Parley.Crm.Contracts.Metrics;

/// <summary>
/// Batch reader of the customer metrics for the organization of the current tenant.
/// <para>
/// Keyset paging by contact id: pass the last <see cref="CrmCustomerMetricsRow.ContactId"/> of the previous batch
/// as <c>afterContactId</c> (null for the first batch). Every live contact is returned, including contacts
/// without orders (zero counts, null dates), in ascending contact id order. Deleted contacts are skipped.
/// </para>
/// <para>
/// Windows and values are computed as of <c>asOf</c>: orders after that instant are ignored.
/// <c>limit</c> must be between 1 and <see cref="CrmCustomerMetricsContract.MaximumBatchSize"/>;
/// otherwise <see cref="ArgumentOutOfRangeException"/> is thrown.
/// </para>
/// <para>
/// Platform reader: it reads every live contact of the tenant without the user data scopes, because its callers are
/// platform jobs, not users. It must never back a user-facing endpoint; user-facing reads go through the scoped handlers.
/// </para>
/// </summary>
public interface ICrmCustomerMetricsReader
{
    Task<IReadOnlyList<CrmCustomerMetricsRow>> ReadBatchAsync(
        Guid? afterContactId,
        int limit,
        DateTimeOffset asOf,
        CancellationToken cancellationToken);
}
