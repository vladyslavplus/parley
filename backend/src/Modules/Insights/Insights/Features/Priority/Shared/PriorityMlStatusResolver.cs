using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Priority.Shared;

/// <summary>
/// The ML status a priority list would get from the switch, the effective uplift model and the completed uplift runs. The
/// build stamps this status with the rows it writes, and the list reads it before the first complete day.
/// </summary>
public static class PriorityMlStatusResolver
{
    public static async Task<string> ReadAsync(InsightsDbContext dbContext, CancellationToken cancellationToken)
    {
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.MlEnabled, row.ActiveUpliftVersion })
            .FirstOrDefaultAsync(cancellationToken);
        var model = await ActiveModelResolver.FindAsync(dbContext, ScoreKinds.Uplift, setting?.ActiveUpliftVersion, cancellationToken);

        // Ready needs a completed run of the version in use now: scores of an earlier model do not make the list ML.
        var activeRun = await ActiveScoringRuns.FindActiveRunIdAsync(dbContext, ScoreKinds.Uplift, cancellationToken);
        return PriorityMlStatus.Resolve(setting?.MlEnabled ?? false, model?.Version, activeRun is not null);
    }

    /// <summary>
    /// Provenance of the uplift model in use now, for the badge of the list. Null when no uplift model is available; the
    /// caller reports it only for a list that ranked by uplift scores.
    /// </summary>
    public static async Task<string?> ReadActiveUpliftProvenanceAsync(InsightsDbContext dbContext, CancellationToken cancellationToken)
    {
        var version = await dbContext.MlSettings.AsNoTracking()
            .Select(row => row.ActiveUpliftVersion)
            .FirstOrDefaultAsync(cancellationToken);
        var model = await ActiveModelResolver.FindAsync(dbContext, ScoreKinds.Uplift, version, cancellationToken);
        return model?.Provenance;
    }
}
