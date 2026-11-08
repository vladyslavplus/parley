using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.Archive;

public sealed class ArchiveCustomFieldCommandHandler(CrmDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<ArchiveCustomFieldCommand, Result<CustomFieldDefinitionView>>
{
    public async ValueTask<Result<CustomFieldDefinitionView>> Handle(ArchiveCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var definition = await CustomFieldDefinitionViews.FindTrackedAsync(dbContext, command.Id, cancellationToken);
        if (definition is null)
        {
            return Result<CustomFieldDefinitionView>.Failure(Error.NotFound(CrmErrorCodes.CustomFieldNotFound));
        }

        if (definition.ArchivedAt is null)
        {
            definition.ArchivedAt = timeProvider.GetUtcNow().UtcDateTime;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<CustomFieldDefinitionView>.Success(CustomFieldDefinitionViews.ToView(definition));
    }
}
