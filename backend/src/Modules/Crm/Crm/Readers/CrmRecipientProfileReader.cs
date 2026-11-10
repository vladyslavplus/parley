using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Contracts.Recipients;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Readers;

/// <summary>Batch reader of the printable recipient values, for the current organization.</summary>
public sealed class CrmRecipientProfileReader(CrmDbContext dbContext, ITenantContext tenantContext) : ICrmRecipientProfileReader
{
    /// <summary>The send path's profiles: a soft-deleted contact is left out, so it is never mailed.</summary>
    public async Task<IReadOnlyList<CrmRecipientProfileRow>> ReadBatchAsync(
        IReadOnlyCollection<Guid> contactIds,
        CancellationToken cancellationToken) =>
        (await ReadVisibleBatchAsync(contactIds, null, cancellationToken))
            .Where(profile => !profile.IsDeleted)
            .ToArray();

    public async Task<IReadOnlyList<CrmRecipientProfileRow>> ReadVisibleBatchAsync(
        IReadOnlyCollection<Guid> contactIds,
        IReadOnlyCollection<Guid>? visibleOwnerUserIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contactIds);
        if (contactIds.Count is < 1 or > CrmCustomerMetricsContract.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(contactIds));
        }

        var owners = (visibleOwnerUserIds ?? []).ToArray();
        var rows = await dbContext.Database
            .SqlQuery<RecipientProfileSqlRow>(RecipientProfileSql.Batch(
                tenantContext.OrganizationId,
                contactIds.ToArray(),
                owners,
                visibleOwnerUserIds is null))
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new CrmRecipientProfileRow(row.ContactId, row.FirstName, row.LastName, row.CompanyName, row.IsDeleted))
            .ToArray();
    }
}
