using Parley.Crm.Contracts.Metrics;

namespace Parley.Crm.Contracts.Audience;

/// <summary>
/// Keyset batch reader of the contacts that can receive an email: live, with an email address.
/// Segment filters and suppression lists belong to the caller. Batches come in ascending contact id order;
/// pass the last id of a batch as <c>afterContactId</c>. <c>limit</c> must be between 1 and
/// <see cref="CrmCustomerMetricsContract.MaximumBatchSize"/>.
/// <para>
/// Platform reader: it returns the eligible contacts of the tenant without the user data scopes, for platform
/// campaign jobs. It must never back a user-facing endpoint; user-facing reads go through the scoped handlers.
/// </para>
/// </summary>
public interface ICrmAudienceReader
{
    Task<IReadOnlyList<CrmAudienceRow>> ReadEligibleBatchAsync(Guid? afterContactId, int limit, CancellationToken cancellationToken);
}
