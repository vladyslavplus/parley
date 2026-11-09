using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Delete;

/// <summary>
/// Deletes an open stage. Live deals block the delete. Soft-deleted deals still pointing at the stage move to the
/// pipeline's first other open stage, at the end of its board, in the same transaction, so nothing dangles.
/// </summary>
public sealed class DeletePipelineStageCommandHandler(CrmDbContext dbContext) : IRequestHandler<DeletePipelineStageCommand, Result>
{
    public async ValueTask<Result> Handle(DeletePipelineStageCommand command, CancellationToken cancellationToken)
    {
        var stage = await dbContext.PipelineStages.FirstOrDefaultAsync(
            entity => entity.Id == command.StageId && entity.PipelineId == command.PipelineId,
            cancellationToken);
        if (stage is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.StageNotFound));
        }

        if (PipelineStageKinds.IsTerminal(stage.Kind))
        {
            return Result.Failure(Error.Conflict(CrmErrorCodes.StageTerminalFixed));
        }

        if (await dbContext.Deals.AnyAsync(deal => deal.StageId == command.StageId && deal.DeletedAt == null, cancellationToken))
        {
            return Result.Failure(Error.Conflict(CrmErrorCodes.StageHasDeals));
        }

        var openStages = await dbContext.PipelineStages.AsNoTracking()
            .Where(other => other.PipelineId == command.PipelineId && other.Kind == PipelineStageKinds.Open)
            .OrderBy(other => other.Position)
            .Select(other => new { other.Id })
            .ToListAsync(cancellationToken);
        if (openStages.Count <= 1)
        {
            return Result.Failure(Error.Conflict(CrmErrorCodes.StageOpenRequired));
        }

        var target = openStages.First(other => other.Id != stage.Id).Id;
        try
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await MoveDeletedDealsAsync(command.StageId, target, cancellationToken);
            dbContext.PipelineStages.Remove(stage);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.PipelineStagePositionIndex))
        {
            // A parallel stage write took the same position: the caller retries against the current order.
            return Result.Failure(ConcurrencyErrors.ToConflictError());
        }

        return Result.Success();
    }

    /// <summary>Moves the soft-deleted deals of a stage to the end of the target stage's board, one place each.</summary>
    private async Task MoveDeletedDealsAsync(Guid fromStageId, Guid toStageId, CancellationToken cancellationToken)
    {
        var deletedIds = await dbContext.Deals
            .Where(deal => deal.StageId == fromStageId && deal.DeletedAt != null)
            .OrderBy(deal => deal.BoardPosition)
            .ThenBy(deal => deal.Id)
            .Select(deal => deal.Id)
            .ToListAsync(cancellationToken);
        if (deletedIds.Count == 0)
        {
            return;
        }

        var last = await dbContext.Deals
            .Where(deal => deal.StageId == toStageId)
            .MaxAsync(deal => (long?)deal.BoardPosition, cancellationToken) ?? 0;
        for (var index = 0; index < deletedIds.Count; index++)
        {
            var position = last + (index + 1) * CrmLimits.DealBoardPositionGap;
            var dealId = deletedIds[index];
            await dbContext.Deals
                .Where(deal => deal.Id == dealId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(deal => deal.StageId, toStageId).SetProperty(deal => deal.BoardPosition, position),
                    cancellationToken);
        }
    }
}
