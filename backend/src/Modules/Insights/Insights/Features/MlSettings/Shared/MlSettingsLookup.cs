using Microsoft.EntityFrameworkCore;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Insights.Features.MlSettings.Shared;

/// <summary>
/// Reads the ML settings view, with the effective model of each kind from the one resolver the runs use. A missing row reads
/// as ML off with version 0 and no stored choice; the effective models still follow the newest synced versions.
/// </summary>
public static class MlSettingsLookup
{
    public static async Task<MlSettingsView> FindViewAsync(InsightsDbContext dbContext, CancellationToken cancellationToken)
    {
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new
            {
                row.MlEnabled,
                row.ActiveUpliftVersion,
                row.ActiveClvVersion,
                Version = EF.Property<uint>(row, PersistenceConstants.XminPropertyName),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var effective = await ActiveModelResolver.ReadEffectiveAsync(
            dbContext,
            setting?.ActiveUpliftVersion,
            setting?.ActiveClvVersion,
            cancellationToken);

        return setting is null
            ? new MlSettingsView(false, null, null, 0, effective.Uplift, effective.Clv)
            : new MlSettingsView(
                setting.MlEnabled,
                setting.ActiveUpliftVersion,
                setting.ActiveClvVersion,
                setting.Version,
                effective.Uplift,
                effective.Clv);
    }
}
