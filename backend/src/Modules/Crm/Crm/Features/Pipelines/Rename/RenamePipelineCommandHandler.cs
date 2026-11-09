using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Pipelines.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Shared.Text;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Pipelines.Rename;

public sealed class RenamePipelineCommandHandler(CrmDbContext dbContext) : IRequestHandler<RenamePipelineCommand, Result<PipelineView>>
{
    public async ValueTask<Result<PipelineView>> Handle(RenamePipelineCommand command, CancellationToken cancellationToken)
    {
        var pipeline = await dbContext.Pipelines.FirstOrDefaultAsync(entity => entity.Id == command.Id, cancellationToken);
        if (pipeline is null)
        {
            return Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound));
        }

        pipeline.Rename(TextValues.Required(command.Name), PipelineNames.Display(pipeline.Name, pipeline.SystemKey));

        // The stale-write check: the UPDATE matches the client's row version, so a changed row affects nothing.
        var entry = dbContext.Entry(pipeline);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<PipelineView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        var view = await PipelineReader.ReadAsync(dbContext, command.Id, cancellationToken);
        return view is null
            ? Result<PipelineView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound))
            : Result<PipelineView>.Success(view);
    }
}
