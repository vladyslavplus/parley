using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Scoring.Run;

/// <summary>
/// The scoring run of one organization, enqueued by the start command. Runs under the tenant of the run, so every read and
/// write of the run is scoped to its organization.
/// </summary>
public sealed class RunScoringJob(IMutableTenantContext tenantContext, ScoringRunRunner runner) : TenantBackgroundJob(tenantContext)
{
    private Guid _runId;

    /// <summary>Entry point enqueued by the start command. The run id is the only argument that differs between runs.</summary>
    public Task ExecuteAsync(Guid organizationId, Guid runId, CancellationToken cancellationToken)
    {
        _runId = runId;
        return ExecuteAsync(organizationId, cancellationToken);
    }

    protected override Task RunAsync(CancellationToken cancellationToken) => runner.RunAsync(_runId, cancellationToken);
}
