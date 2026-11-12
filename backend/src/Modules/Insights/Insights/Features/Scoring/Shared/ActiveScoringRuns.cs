using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scoring.Shared;

/// <summary>
/// The scores that count as current. A kind counts only while ML is switched on, and only the runs that scored with the version
/// in use now count: a switch from one model to another makes the old scores non-current until a run with the new model completes.
/// </summary>
public static class ActiveScoringRuns
{
    /// <summary>The version the kind scores with now, or null while ML is off or the registry holds no version of the kind.</summary>
    private static async Task<string?> ReadActiveVersionAsync(InsightsDbContext dbContext, string kind, CancellationToken cancellationToken)
    {
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.MlEnabled, row.ActiveUpliftVersion, row.ActiveClvVersion })
            .FirstOrDefaultAsync(cancellationToken);
        if (setting is not { MlEnabled: true })
        {
            return null;
        }

        var configured = kind == ScoreKinds.Uplift ? setting.ActiveUpliftVersion : setting.ActiveClvVersion;
        var model = await ActiveModelResolver.FindAsync(dbContext, kind, configured, cancellationToken);
        return model?.Version;
    }

    /// <summary>The newest completed run of the kind that scored with the version in use now, or null when there is none.</summary>
    public static async Task<Guid?> FindActiveRunIdAsync(InsightsDbContext dbContext, string kind, CancellationToken cancellationToken)
    {
        var version = await ReadActiveVersionAsync(dbContext, kind, cancellationToken);
        if (version is null)
        {
            return null;
        }

        return await dbContext.ScoringRuns.AsNoTracking()
            .Where(run => run.Kind == kind && run.Status == ScoringRunStatuses.Completed && run.ModelVersion == version)
            .OrderByDescending(run => run.FinishedAt)
            .Select(run => (Guid?)run.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
