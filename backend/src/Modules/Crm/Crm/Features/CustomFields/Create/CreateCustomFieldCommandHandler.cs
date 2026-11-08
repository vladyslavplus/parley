using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.Create;

public sealed class CreateCustomFieldCommandHandler(CrmDbContext dbContext)
    : IRequestHandler<CreateCustomFieldCommand, Result<CustomFieldDefinitionView>>
{
    public async ValueTask<Result<CustomFieldDefinitionView>> Handle(CreateCustomFieldCommand command, CancellationToken cancellationToken)
    {
        // Archived definitions keep their values but no longer count against the limit.
        var definitionCount = await dbContext.CustomFieldDefinitions
            .CountAsync(definition => definition.EntityType == command.EntityType && definition.ArchivedAt == null, cancellationToken);
        if (definitionCount >= CrmLimits.CustomFieldsPerEntityMaximumCount)
        {
            return Result<CustomFieldDefinitionView>.Failure(Error.Conflict(CrmErrorCodes.CustomFieldLimitReached));
        }

        if (await dbContext.CustomFieldDefinitions.AnyAsync(
                definition => definition.EntityType == command.EntityType && definition.Key == command.Key,
                cancellationToken))
        {
            return Result<CustomFieldDefinitionView>.Failure(Error.Conflict(CrmErrorCodes.CustomFieldKeyTaken));
        }

        IReadOnlyList<string> options = command.FieldType == CustomFieldTypes.Select && command.Options is not null ? command.Options : [];
        var definition = new CustomFieldDefinition
        {
            EntityType = command.EntityType,
            Key = command.Key,
            Label = command.Label.Trim(),
            FieldType = command.FieldType,
            Options = JsonSerializer.Serialize(options),
            IsRequired = command.IsRequired,
            Position = command.Position,
        };
        dbContext.CustomFieldDefinitions.Add(definition);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.CustomFieldKeyIndex))
        {
            return Result<CustomFieldDefinitionView>.Failure(Error.Conflict(CrmErrorCodes.CustomFieldKeyTaken));
        }

        return Result<CustomFieldDefinitionView>.Success(CustomFieldDefinitionViews.ToView(definition));
    }
}
