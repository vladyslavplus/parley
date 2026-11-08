using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.CustomFields.List;

public sealed class ListCustomFieldsQueryHandler(CrmDbContext dbContext)
    : IRequestHandler<ListCustomFieldsQuery, Result<IReadOnlyList<CustomFieldDefinitionView>>>
{
    public async ValueTask<Result<IReadOnlyList<CustomFieldDefinitionView>>> Handle(ListCustomFieldsQuery query, CancellationToken cancellationToken)
    {
        var definitions = dbContext.CustomFieldDefinitions.AsNoTracking();
        if (query.EntityType is not null)
        {
            definitions = definitions.Where(definition => definition.EntityType == query.EntityType);
        }

        var rows = await definitions
            .OrderBy(definition => definition.EntityType)
            .ThenBy(definition => definition.Position)
            .ThenBy(definition => definition.Key)
            .ToListAsync(cancellationToken);
        IReadOnlyList<CustomFieldDefinitionView> views = rows.Select(CustomFieldDefinitionViews.ToView).ToArray();
        return Result<IReadOnlyList<CustomFieldDefinitionView>>.Success(views);
    }
}
