using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Audience;
using Parley.Crm.Contracts.Metrics;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Readers;

/// <summary>Keyset batches of the live contacts that have an email address, for the current organization.</summary>
public sealed class CrmAudienceReader(CrmDbContext dbContext, ITenantContext tenantContext) : ICrmAudienceReader
{
    public async Task<IReadOnlyList<CrmAudienceRow>> ReadEligibleBatchAsync(
        Guid? afterContactId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > CrmCustomerMetricsContract.MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        var rows = await dbContext.Database
            .SqlQuery<AudienceSqlRow>(AudienceSql.Batch(tenantContext.OrganizationId, afterContactId, limit))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new CrmAudienceRow(row.ContactId, row.OwnerUserId, row.CountryIso2)).ToArray();
    }
}
