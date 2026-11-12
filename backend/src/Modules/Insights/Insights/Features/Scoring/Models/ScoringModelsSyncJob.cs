using Microsoft.Extensions.Logging;
using Parley.Insights.Features.Scoring.Client;
using Parley.SharedKernel.BackgroundJobs;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Scoring.Models;

/// <summary>
/// Platform-wide sync of the model registry, on the recurring schedule and when a run start finds no model. A service that
/// cannot be reached is logged and skipped; the next scheduled sync tries again.
/// </summary>
public sealed class ScoringModelsSyncJob(
    IMutableTenantContext tenantContext,
    ScoringModelsSync sync,
    ILogger<ScoringModelsSyncJob> logger) : TenantBackgroundJob(tenantContext)
{
    protected override bool PlatformWide => true;

    protected override async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await sync.SyncAsync(cancellationToken);
        }
        catch (ScoringServiceException exception)
        {
            logger.LogWarning("Scoring model sync skipped: the service answered with {ErrorCode}.", exception.Code);
        }
    }
}
