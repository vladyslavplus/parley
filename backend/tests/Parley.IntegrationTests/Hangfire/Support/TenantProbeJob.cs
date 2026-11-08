using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Hangfire.Support;

/// <summary>
/// Test-only job proving the worker restores tenant context from arguments.
/// </summary>
public sealed class TenantProbeJob(IMutableTenantContext tenantContext, ProbeRecorder recorder)
    : TenantBackgroundJob(tenantContext)
{
    protected override Task RunAsync(CancellationToken cancellationToken)
    {
        recorder.Record(TenantContext.OrganizationId);
        return Task.CompletedTask;
    }
}
