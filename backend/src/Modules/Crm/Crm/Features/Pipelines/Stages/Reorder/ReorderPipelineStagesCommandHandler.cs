using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Reorder;

public sealed class ReorderPipelineStagesCommandHandler(CrmDbContext dbContext)
    : IRequestHandler<ReorderPipelineStagesCommand, Result<PipelineView>>
{
    public async ValueTask<Result<PipelineView>> Handle(ReorderPipelineStagesCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pipelineExists = await dbContext.Pipelines.AnyAsync(pipeline => pipeline.Id == command.PipelineId, cancellationToken);
        if (!pipelineExists)
        {
            return Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound));
        }

        var stages = await dbContext.PipelineStages.AsNoTracking()
            .Where(stage => stage.PipelineId == command.PipelineId)
            .OrderBy(stage => stage.Position)
            .Select(stage => new { stage.Id, stage.Kind })
            .ToListAsync(cancellationToken);

        var openIds = stages.Where(stage => stage.Kind == PipelineStageKinds.Open).Select(stage => stage.Id).ToList();
        var requested = command.StageIds.ToHashSet();
        if (command.StageIds.Count != openIds.Count || requested.Count != openIds.Count || !requested.SetEquals(openIds))
        {
            return Result<PipelineView>.Failure(CrmErrors.Field(nameof(ReorderPipelineStagesCommand.StageIds), CrmErrorCodes.StageOrderInvalid));
        }

        var ordered = command.StageIds
            .Concat(stages.Where(stage => PipelineStageKinds.IsTerminal(stage.Kind)).Select(stage => stage.Id))
            .ToList();
        try
        {
            await PipelineStageOrder.WriteAsync(dbContext, ordered, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.PipelineStagePositionIndex))
        {
            // A parallel stage write took the same position: the caller retries against the current order.
            return Result<PipelineView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        var view = await PipelineReader.ReadAsync(dbContext, command.PipelineId, cancellationToken);
        return view is null
            ? Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound))
            : Result<PipelineView>.Success(view);
    }
}
