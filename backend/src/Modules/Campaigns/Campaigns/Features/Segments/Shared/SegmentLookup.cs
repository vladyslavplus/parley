using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Domain.Segments;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Features.Segments.Shared;

/// <summary>Segment lookups shared by the segment handlers. Archived segments are not found.</summary>
internal static class SegmentLookup
{
    public static Task<Segment?> FindLiveTrackedAsync(CampaignsDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Segments.FirstOrDefaultAsync(segment => segment.Id == id && segment.ArchivedAt == null, cancellationToken);

    /// <summary>Reads the view from the database, so the version is the one the database holds after a save.</summary>
    public static Task<SegmentView?> FindLiveViewAsync(CampaignsDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Segments
            .AsNoTracking()
            .Where(segment => segment.Id == id && segment.ArchivedAt == null)
            .Select(segment => new SegmentView(
                segment.Id,
                segment.Name,
                segment.Definition,
                EF.Property<uint>(segment, PersistenceConstants.XminPropertyName)))
            .FirstOrDefaultAsync(cancellationToken);
}
