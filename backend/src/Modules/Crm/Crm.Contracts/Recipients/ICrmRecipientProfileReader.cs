using Parley.Crm.Contracts.Metrics;

namespace Parley.Crm.Contracts.Recipients;

/// <summary>
/// Reads the printable values of campaign recipients in one batch. Only live (not soft-deleted) contacts of the
/// current organization are returned; a requested contact that is missing from the result is no longer sendable, or it is
/// outside the caller's owner scope. The campaigns send job reads without an owner scope (a platform job); the campaign
/// recipients list passes the caller's visible owners, so a user never reads the name of a contact outside their scope.
/// Both read names only, never statistics. The contact ids must number at most
/// <see cref="CrmCustomerMetricsContract.MaximumBatchSize"/>.
/// </summary>
public interface ICrmRecipientProfileReader
{
    /// <summary>Profiles of the contacts, with no owner scope: for platform jobs only.</summary>
    Task<IReadOnlyList<CrmRecipientProfileRow>> ReadBatchAsync(IReadOnlyCollection<Guid> contactIds, CancellationToken cancellationToken);

    /// <summary>
    /// Profiles of the contacts whose owner is in <paramref name="visibleOwnerUserIds"/>. Null means the whole organization. An
    /// unowned contact is visible only when the owner list is null, the same rule the data scopes use.
    /// </summary>
    Task<IReadOnlyList<CrmRecipientProfileRow>> ReadVisibleBatchAsync(
        IReadOnlyCollection<Guid> contactIds,
        IReadOnlyCollection<Guid>? visibleOwnerUserIds,
        CancellationToken cancellationToken);
}
