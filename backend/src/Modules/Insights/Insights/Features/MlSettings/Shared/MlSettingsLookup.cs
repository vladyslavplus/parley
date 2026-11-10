using Microsoft.EntityFrameworkCore;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Insights.Features.MlSettings.Shared;

/// <summary>Reads the ML settings view. A missing row reads as ML off with version 0.</summary>
public static class MlSettingsLookup
{
    public static async Task<MlSettingsView> FindViewAsync(InsightsDbContext dbContext, CancellationToken cancellationToken) =>
        await dbContext.MlSettings.AsNoTracking()
            .Select(setting => new MlSettingsView(
                setting.MlEnabled,
                setting.ActiveUpliftVersion,
                setting.ActiveClvVersion,
                EF.Property<uint>(setting, PersistenceConstants.XminPropertyName)))
            .FirstOrDefaultAsync(cancellationToken)
        ?? new MlSettingsView(false, null, null, 0);
}
