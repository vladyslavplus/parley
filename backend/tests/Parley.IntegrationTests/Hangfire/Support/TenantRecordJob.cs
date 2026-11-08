using Parley.IntegrationTests.Tenancy.Support;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Hangfire.Support;

/// <summary>
/// Test-only job that saves one tenant row through the DI-resolved DbContext,
/// proving a worker-run job persists under its organization in the real host.
/// </summary>
public sealed class TenantRecordJob(IMutableTenantContext tenantContext, TenantTestDbContext dbContext)
    : TenantBackgroundJob(tenantContext)
{
    public const string RecordName = "hangfire-job-record";

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        dbContext.TenantRecords.Add(new TenantRecord(Guid.Empty, RecordName));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
