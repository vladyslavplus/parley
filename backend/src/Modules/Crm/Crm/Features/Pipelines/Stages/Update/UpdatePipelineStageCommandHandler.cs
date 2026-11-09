using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Shared.Text;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Stages.Update;

public sealed class UpdatePipelineStageCommandHandler(CrmDbContext dbContext) : IRequestHandler<UpdatePipelineStageCommand, Result<PipelineView>>
{
    public async ValueTask<Result<PipelineView>> Handle(UpdatePipelineStageCommand command, CancellationToken cancellationToken)
    {
        var stage = await dbContext.PipelineStages.FirstOrDefaultAsync(
            entity => entity.Id == command.StageId && entity.PipelineId == command.PipelineId,
            cancellationToken);
        if (stage is null)
        {
            return Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.StageNotFound));
        }

        var name = TextValues.Required(command.Name);
        var normalizedName = name.ToLowerInvariant();
        var nameTaken = await dbContext.PipelineStages.AnyAsync(
            other => other.PipelineId == command.PipelineId && other.Id != command.StageId && other.NameNormalized == normalizedName,
            cancellationToken);
        if (nameTaken)
        {
            return Result<PipelineView>.Failure(Error.Conflict(CrmErrorCodes.StageNameTaken));
        }

        stage.Rename(name, PipelineNames.Display(stage.Name, stage.SystemKey));
        stage.ColorIndex = command.ColorIndex;
        stage.ProbabilityBps = command.ProbabilityBps;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.PipelineStageNameIndex))
        {
            return Result<PipelineView>.Failure(Error.Conflict(CrmErrorCodes.StageNameTaken));
        }

        var view = await PipelineReader.ReadAsync(dbContext, command.PipelineId, cancellationToken);
        return view is null
            ? Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound))
            : Result<PipelineView>.Success(view);
    }
}
