using Microsoft.EntityFrameworkCore;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Exports;

/// <summary>
/// Recurring sweep of expired contact exports: deletes each export file, then its row. It is
/// platform-wide (it covers every organization) and idempotent: a file or row already gone is
/// skipped. A failed file delete keeps the row, so the next pass retries it.
/// </summary>
public sealed class ContactExportCleanupJob(
    IMutableTenantContext tenantContext,
    CrmDbContext dbContext,
    ContactExportStorage storage,
    TimeProvider timeProvider) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expired = await dbContext.ContactExports.IgnoreQueryFilters()
            .Where(export => export.ExpiresAt <= now)
            .OrderBy(export => export.ExpiresAt)
            .ThenBy(export => export.Id)
            .Take(CrmLimits.ContactExportCleanupBatchSize)
            .Select(export => new { export.Id, export.FileName })
            .ToListAsync(cancellationToken);

        var removed = new List<Guid>(expired.Count);
        foreach (var export in expired)
        {
            if (export.FileName is not null)
            {
                var path = storage.PathFor(export.FileName);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            removed.Add(export.Id);
        }

        if (removed.Count > 0)
        {
            await dbContext.ContactExports.IgnoreQueryFilters()
                .Where(export => removed.Contains(export.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
