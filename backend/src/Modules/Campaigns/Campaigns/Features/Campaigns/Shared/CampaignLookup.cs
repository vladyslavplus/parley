using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Persistence.Conventions;

namespace Parley.Campaigns.Features.Campaigns.Shared;

/// <summary>Campaign lookups shared by the campaign handlers.</summary>
internal static class CampaignLookup
{
    public static Task<Campaign?> FindTrackedAsync(CampaignsDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Campaigns.FirstOrDefaultAsync(campaign => campaign.Id == id, cancellationToken);

    /// <summary>Reads the view from the database, so the version is the one the database holds after a save.</summary>
    public static Task<CampaignView?> FindViewAsync(CampaignsDbContext dbContext, Guid id, CancellationToken cancellationToken) =>
        dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == id)
            .Select(campaign => new CampaignView(
                campaign.Id,
                campaign.Name,
                campaign.TemplateId,
                campaign.SegmentId,
                campaign.Status,
                campaign.HoldoutShareBps,
                campaign.HoldoutSeed,
                campaign.TargetingMode,
                campaign.TargetingShareBps,
                campaign.AudienceFrozenAt,
                EF.Property<uint>(campaign, PersistenceConstants.XminPropertyName)))
            .FirstOrDefaultAsync(cancellationToken);
}
