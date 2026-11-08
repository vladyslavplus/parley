using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Tags.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Text;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Tags.Update;

public sealed class UpdateTagCommandHandler(CrmDbContext dbContext) : IRequestHandler<UpdateTagCommand, Result<TagView>>
{
    public async ValueTask<Result<TagView>> Handle(UpdateTagCommand command, CancellationToken cancellationToken)
    {
        var tag = await TagLookup.FindTrackedAsync(dbContext, command.Id, cancellationToken);
        if (tag is null)
        {
            return Result<TagView>.Failure(Error.NotFound(CrmErrorCodes.TagNotFound));
        }

        var name = TextValues.Required(command.Name);
        var normalized = name.ToLowerInvariant();
        if (await dbContext.Tags.AnyAsync(other => other.NameNormalized == normalized && other.Id != tag.Id, cancellationToken))
        {
            return Result<TagView>.Failure(Error.Conflict(CrmErrorCodes.TagNameTaken));
        }

        tag.Name = name;
        tag.ColorIndex = command.ColorIndex;
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.TagNameIndex))
        {
            return Result<TagView>.Failure(Error.Conflict(CrmErrorCodes.TagNameTaken));
        }

        return Result<TagView>.Success(TagLookup.ToView(tag));
    }
}
