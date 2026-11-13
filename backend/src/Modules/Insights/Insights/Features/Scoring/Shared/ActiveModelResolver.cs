using Microsoft.EntityFrameworkCore;
using Parley.Insights.Contracts.Scores;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Persistence;

namespace Parley.Insights.Features.Scoring.Shared;

/// <summary>
/// The model a score kind is scored with. The configured active version wins when the organization set one; otherwise the
/// newest available version of the kind is used. A version the last successful sync did not report as available is never
/// returned, and the automatic choice skips a stub: a stub only boots the service, so it must not rank real contacts unless
/// an administrator activates it by hand. Ties on the training instant break on the version, so the answer is stable.
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

        var models = await dbContext.ModelVersions.AsNoTracking()
            .Where(model => model.ModelName == modelName && model.IsAvailable)
            .ToListAsync(cancellationToken);
        return Select(models, configuredVersion);
    }

    /// <summary>
    /// The choice among the available versions of one kind: the configured version when it is among them, otherwise the
    /// newest non-stub one, ties broken by the version. Pure, so the rule is tested without a database.
    /// </summary>
    public static ModelVersion? Select(IReadOnlyList<ModelVersion> available, string? configuredVersion)
    {
        ArgumentNullException.ThrowIfNull(available);
        var candidates = string.IsNullOrWhiteSpace(configuredVersion)
            ? available.Where(model => model.Provenance != ScoringProvenances.Stub)
            : available.Where(model => model.Version == configuredVersion);
        return candidates
            .OrderByDescending(model => model.TrainedAt)
            .ThenByDescending(model => model.Version, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    /// <summary>
    /// The effective active version of both kinds, resolved by the same rule every reader uses: the stored choice when it is
    /// available in the registry, otherwise the newest non-stub version. A stored choice the registry does not hold gives no
    /// version for its kind; it is never replaced by another. The settings, the registry and the runs all read this one answer.
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
