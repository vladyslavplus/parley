using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Tags.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.Delete;

public sealed class DeleteTagCommandHandler(CrmDbContext dbContext) : IRequestHandler<DeleteTagCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await TagLookup.FindTrackedAsync(dbContext, command.Id, cancellationToken);
        if (tag is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.TagNotFound));
        }

        dbContext.Tags.Remove(tag);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
