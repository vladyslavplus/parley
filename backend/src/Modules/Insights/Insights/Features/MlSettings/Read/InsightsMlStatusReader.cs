using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.MlStatus;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.MlSettings.Read;

/// <summary>The ML switch and the active model versions of the tenant. A missing settings row means ML is off.</summary>
public sealed class InsightsMlStatusReader(InsightsDbContext dbContext) : IMlStatusReader
{
    public async Task<MlStatus> GetAsync(CancellationToken cancellationToken)
    {
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.MlEnabled, row.ActiveUpliftVersion, row.ActiveClvVersion })
            .FirstOrDefaultAsync(cancellationToken);
        return setting is null
            ? new MlStatus(false, null, null)
            : new MlStatus(setting.MlEnabled, setting.ActiveUpliftVersion, setting.ActiveClvVersion);
    }
}
