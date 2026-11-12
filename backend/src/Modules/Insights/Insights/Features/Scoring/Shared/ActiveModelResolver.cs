using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scoring.Shared;

/// <summary>
/// The model a score kind is scored with. The configured active version wins when the organization set one; otherwise the
/// newest synced version of the kind is used. A version the registry does not hold is never returned, so a run can only
/// pin a model the sync has seen.
/// </summary>
public static class ActiveModelResolver
{
    public static async Task<ModelVersion?> FindAsync(
        InsightsDbContext dbContext,
        string kind,
        string? configuredVersion,
        CancellationToken cancellationToken)
    {
        var modelName = ScoringRunRules.ModelNameOf(kind);
        if (modelName is null)
        {
            return null;
        }

        var models = dbContext.ModelVersions.AsNoTracking().Where(model => model.ModelName == modelName);
        if (!string.IsNullOrWhiteSpace(configuredVersion))
        {
            models = models.Where(model => model.Version == configuredVersion);
        }

        return await models
            .OrderByDescending(model => model.TrainedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// The effective active version of both kinds, resolved by the same rule every reader uses: the stored choice when it is
    /// in the registry, otherwise the newest synced version. The settings, the registry and the runs all read this one answer.
    /// </summary>
    public static async Task<EffectiveModelVersions> ReadEffectiveAsync(
        InsightsDbContext dbContext,
        string? configuredUplift,
        string? configuredClv,
        CancellationToken cancellationToken)
    {
        var uplift = await FindAsync(dbContext, ScoreKinds.Uplift, configuredUplift, cancellationToken);
        var clv = await FindAsync(dbContext, ScoreKinds.Clv, configuredClv, cancellationToken);
        return new EffectiveModelVersions(uplift?.Version, clv?.Version);
    }
}
