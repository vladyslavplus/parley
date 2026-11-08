using System.Text.Json;
using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.Update;

public sealed class UpdateCustomFieldCommandHandler(CrmDbContext dbContext)
    : IRequestHandler<UpdateCustomFieldCommand, Result<CustomFieldDefinitionView>>
{
    public async ValueTask<Result<CustomFieldDefinitionView>> Handle(UpdateCustomFieldCommand command, CancellationToken cancellationToken)
    {
        var definition = await CustomFieldDefinitionViews.FindTrackedAsync(dbContext, command.Id, cancellationToken);
        if (definition is null)
        {
            return Result<CustomFieldDefinitionView>.Failure(Error.NotFound(CrmErrorCodes.CustomFieldNotFound));
        }

        if (definition.ArchivedAt is not null)
        {
            return Result<CustomFieldDefinitionView>.Failure(Error.Conflict(CrmErrorCodes.CustomFieldArchived));
        }

        if (definition.FieldType == CustomFieldTypes.Select)
        {
            if (command.Options is not { Count: > 0 })
            {
                return Result<CustomFieldDefinitionView>.Failure(CrmErrors.Field(nameof(UpdateCustomFieldCommand.Options), CrmErrorCodes.CustomFieldOptionInvalid));
            }

            definition.Options = JsonSerializer.Serialize(command.Options);
        }
        else if (command.Options is { Count: > 0 })
        {
            return Result<CustomFieldDefinitionView>.Failure(CrmErrors.Field(nameof(UpdateCustomFieldCommand.Options), CrmErrorCodes.CustomFieldOptionInvalid));
        }

        definition.Label = command.Label.Trim();
        definition.IsRequired = command.IsRequired;
        definition.Position = command.Position;
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<CustomFieldDefinitionView>.Success(CustomFieldDefinitionViews.ToView(definition));
    }
}
