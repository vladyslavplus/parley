using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Persistence;

namespace Parley.Crm.Features.CustomFields.Shared;

/// <summary>Mapping and lookups of custom field definitions.</summary>
internal static class CustomFieldDefinitionViews
{
    public static CustomFieldDefinitionView ToView(CustomFieldDefinition definition) => new(
        definition.Id,
        definition.EntityType,
        definition.Key,
        definition.Label,
        definition.FieldType,
        JsonSerializer.Deserialize<List<string>>(definition.Options) ?? [],
        definition.IsRequired,
        definition.Position,
        definition.ArchivedAt);

    public static Task<CustomFieldDefinition?> FindTrackedAsync(CrmDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.CustomFieldDefinitions.FirstOrDefaultAsync(definition => definition.Id == id, cancellationToken);
}
