using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.Scoring.Shared;
using Parley.Insights.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Scoring.Models;

public sealed class ListScoringModelsQueryHandler(InsightsDbContext dbContext)
    : IRequestHandler<ListScoringModelsQuery, Result<IReadOnlyList<ScoringModelView>>>
{
    public async ValueTask<Result<IReadOnlyList<ScoringModelView>>> Handle(ListScoringModelsQuery query, CancellationToken cancellationToken)
    {
        var setting = await dbContext.MlSettings.AsNoTracking()
            .Select(row => new { row.ActiveUpliftVersion, row.ActiveClvVersion })
            .FirstOrDefaultAsync(cancellationToken);
        var effective = await ActiveModelResolver.ReadEffectiveAsync(
            dbContext,
            setting?.ActiveUpliftVersion,
            setting?.ActiveClvVersion,
            cancellationToken);

        var models = await dbContext.ModelVersions.AsNoTracking()
            .OrderBy(row => row.ModelName)
            .ThenByDescending(row => row.TrainedAt)
            .ToListAsync(cancellationToken);
        var views = models
            .Select(model => new ScoringModelView(
                model.ModelName,
                model.Version,
                model.Kind,
                model.FeatureSchemaVersion,
                model.Provenance,
                model.TrainedAt,
                model.SyncedAt,
                JsonDocument.Parse(model.Metrics).RootElement.Clone(),
                model.IsAvailable,
                model.IsAvailable && IsActive(model, effective)))
            .ToList();
        return Result<IReadOnlyList<ScoringModelView>>.Success(views);
    }

    /// <summary>A model is in use when it is the effective version of the kind it scores, the same answer the runs get.</summary>
    private static bool IsActive(ModelVersion model, EffectiveModelVersions effective) => model.ModelName switch
    {
        ScoringModelNames.Uplift => model.Version == effective.Uplift,
        ScoringModelNames.Clv => model.Version == effective.Clv,
        _ => false,
    };
}
