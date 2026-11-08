using Microsoft.EntityFrameworkCore;
using Parley.Crm.Domain.Tags;
using Parley.Crm.Persistence;

namespace Parley.Crm.Features.Tags.Shared;

/// <summary>Tag lookups and mapping shared by the tag handlers.</summary>
internal static class TagLookup
{
    public static Task<Tag?> FindTrackedAsync(CrmDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Tags.FirstOrDefaultAsync(tag => tag.Id == id, cancellationToken);

    public static TagView ToView(Tag tag) => new(tag.Id, tag.Name, tag.ColorIndex);

    public static TagView ToView(TagRow row) => new(row.Id, row.Name, row.ColorIndex);
}
