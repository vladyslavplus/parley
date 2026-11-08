using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Persistence;

namespace Parley.Crm.Shared.CustomFields;

/// <summary>Reads the definitions of one record type that accept values (not archived).</summary>
public static class ActiveCustomFieldDefinitions
{
    public static Task<List<CustomFieldDefinition>> ReadAsync(CrmDbContext dbContext, string entityType, CancellationToken cancellationToken) =>
        dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Where(definition => definition.EntityType == entityType && definition.ArchivedAt == null)
            .OrderBy(definition => definition.Position)
            .ThenBy(definition => definition.Key)
            .ToListAsync(cancellationToken);
}
