using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Shared.Text;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Pipelines.Stages.Create;

public sealed class CreatePipelineStageCommandHandler(CrmDbContext dbContext, ITenantContext tenantContext)
    : IRequestHandler<CreatePipelineStageCommand, Result<PipelineView>>
{
    public async ValueTask<Result<PipelineView>> Handle(CreatePipelineStageCommand command, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var pipelineExists = await dbContext.Pipelines.AnyAsync(pipeline => pipeline.Id == command.PipelineId, cancellationToken);
        if (!pipelineExists)
        {
            return Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound));
        }

        var stages = await dbContext.PipelineStages
            .Where(stage => stage.PipelineId == command.PipelineId)
            .OrderBy(stage => stage.Position)
            .ToListAsync(cancellationToken);
        if (stages.Count >= CrmLimits.PipelineStageMaximumCount)
        {
            return Result<PipelineView>.Failure(Error.Conflict(CrmErrorCodes.StageLimitReached));
        }

        var name = TextValues.Required(command.Name);
        var normalizedName = name.ToLowerInvariant();
        if (stages.Exists(stage => stage.NameNormalized == normalizedName))
        {
            return Result<PipelineView>.Failure(Error.Conflict(CrmErrorCodes.StageNameTaken));
        }

        var stage = new PipelineStage
        {
            OrganizationId = tenantContext.OrganizationId,
            PipelineId = command.PipelineId,
            Name = name,
            Kind = PipelineStageKinds.Open,
            ProbabilityBps = command.ProbabilityBps,
            ColorIndex = command.ColorIndex,
            Position = CrmLimits.TemporaryStagePosition,
        };
        dbContext.PipelineStages.Add(stage);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.PipelineStageNameIndex))
        {
            return Result<PipelineView>.Failure(Error.Conflict(CrmErrorCodes.StageNameTaken));
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.PipelineStagePositionIndex))
        {
            // A parallel stage write took the same position: the caller retries against the current order.
            return Result<PipelineView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        // The new open stage goes after the open stages and before the won and lost stages.
        var ordered = stages
            .Where(existing => existing.Kind == PipelineStageKinds.Open)
            .Select(existing => existing.Id)
            .Append(stage.Id)
            .Concat(stages.Where(existing => PipelineStageKinds.IsTerminal(existing.Kind)).Select(existing => existing.Id))
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
