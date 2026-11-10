using Parley.Crm.Contracts.Metrics;

namespace Parley.Crm.Contracts.Audience;

/// <summary>
/// Keyset batch reader of the contacts that can receive an email: live, with an email address.
/// Batches come in ascending contact id order; pass the last id of a batch as <c>afterContactId</c>.
/// <c>limit</c> must be between 1 and <see cref="CrmCustomerMetricsContract.MaximumBatchSize"/>.
/// Segment filters and suppression lists belong to the caller.
/// <para>
/// Sanctioned use: the reader returns the contacts of the tenant without the user data scopes, so it backs
/// campaign audience counts and the campaigns audience freeze. A user-facing endpoint may call it only for an
/// aggregate count and a small sample of identifiers, and only for holders of the campaigns manage permission,
/// who already read every contact. Any other user-facing read goes through the scoped handlers.
/// </para>
/// </summary>
public interface ICrmAudienceReader
{
    Task<IReadOnlyList<CrmAudienceRow>> ReadEligibleBatchAsync(Guid? afterContactId, int limit, CancellationToken cancellationToken);
}
