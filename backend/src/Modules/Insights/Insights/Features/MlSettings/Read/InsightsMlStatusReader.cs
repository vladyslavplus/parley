using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.MlStatus;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.MlSettings.Read;

/// <summary>
/// The ML switch and the model versions in use for the tenant, from the same resolver the runs use. A missing settings row means
/// ML is off, and the models in use follow the newest synced versions.
/// </summary>
public sealed class InsightsMlStatusReader(InsightsDbContext dbContext) : IMlStatusReader
{
    public async Task<MlStatus> GetAsync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.MlEnabled, row.ActiveUpliftVersion, row.ActiveClvVersion })
            .FirstOrDefaultAsync(cancellationToken);
        var effective = await ActiveModelResolver.ReadEffectiveAsync(
            dbContext,
            setting?.ActiveUpliftVersion,
            setting?.ActiveClvVersion,
            cancellationToken);
        return new MlStatus(setting?.MlEnabled ?? false, effective.Uplift, effective.Clv);
    }
}
