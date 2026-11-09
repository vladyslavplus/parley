using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Pipelines;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Board;

/// <summary>
/// Three statements whatever the number of deals: the pipeline name, the stages, and one grouped aggregate of the
/// deals the caller may see.
/// </summary>
public sealed class GetPipelineBoardQueryHandler(CrmDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<GetPipelineBoardQuery, Result<PipelineBoardView>>
{
    public async ValueTask<Result<PipelineBoardView>> Handle(GetPipelineBoardQuery query, CancellationToken cancellationToken)
    {
        var pipeline = await dbContext.Pipelines.AsNoTracking()
            .Where(entity => entity.Id == query.PipelineId)
            .Select(entity => new { entity.Name, entity.SystemKey })
            .FirstOrDefaultAsync(cancellationToken);
        if (pipeline is null)
        {
            return Result<PipelineBoardView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound));
        }

        var stages = (await dbContext.PipelineStages.AsNoTracking()
            .Where(stage => stage.PipelineId == query.PipelineId)
            .OrderBy(stage => stage.Position)
            .Select(PipelineStageViews.Projection)
            .ToListAsync(cancellationToken))
            .Select(PipelineStageViews.ToView)
            .ToList();

        var scope = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        var totals = await DealScopeFilter.Apply(dbContext.Deals.AsNoTracking().Where(deal => deal.PipelineId == query.PipelineId), scope)
            .GroupBy(deal => deal.StageId)
            .Select(group => new { StageId = group.Key, Count = group.Count(), Total = group.Sum(deal => deal.Amount) })
            .ToListAsync(cancellationToken);
        var totalsByStage = totals.ToDictionary(total => total.StageId);

        var columns = stages
            .Select(stage => new PipelineBoardStageView(
                stage.Id,
                stage.Name,
                stage.Position,
                stage.Kind,
                stage.ProbabilityBps,
                stage.ColorIndex,
                totalsByStage.TryGetValue(stage.Id, out var total) ? total.Count : 0,
                total?.Total ?? 0m))
            .ToList();
        return Result<PipelineBoardView>.Success(new PipelineBoardView(
            query.PipelineId,
            PipelineNames.Display(pipeline.Name, pipeline.SystemKey),
            columns));
    }
}
