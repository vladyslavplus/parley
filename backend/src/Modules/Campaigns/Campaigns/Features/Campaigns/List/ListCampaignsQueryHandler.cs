using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Campaigns.Features.Campaigns.List;

/// <summary>
/// Reads one keyset page of the campaigns of the current organization. The organization filter is the database
/// context's global filter, so another organization's campaigns never appear.
/// </summary>
public sealed class ListCampaignsQueryHandler(CampaignsDbContext dbContext)
    : IRequestHandler<ListCampaignsQuery, Result<KeysetCursorPage<CampaignSummaryView>>>
{
    /// <summary>Row projection shared by both sort orders. The version is read from the row's xmin.</summary>
    private static readonly Expression<Func<Campaign, CampaignListRow>> RowProjection = campaign => new CampaignListRow(
        new CampaignSummaryView(
            campaign.Id,
            campaign.Name,
            campaign.Status,
            campaign.HoldoutShareBps,
            campaign.TargetingMode,
            campaign.AudienceFrozenAt,
            campaign.StartedAt,
            campaign.CompletedAt,
            EF.Property<uint>(campaign, PersistenceConstants.XminPropertyName),
            campaign.TemplateId,
            campaign.SegmentId,
            campaign.Source),
        campaign.CreatedAt);

    public async ValueTask<Result<KeysetCursorPage<CampaignSummaryView>>> Handle(ListCampaignsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Invalid();
        }

        var campaigns = Filter(dbContext.Campaigns.AsNoTracking(), query);

        var page = query.Sort == CampaignListSortFields.Created
            ? await KeysetPages.ReadAsync(
                campaigns,
                cursor,
                CampaignListSortFields.Created,
                campaign => campaign.CreatedAt,
                campaign => campaign.Id,
                RowProjection,
                row => row.CreatedAt,
                row => row.Summary.Id,
                KeysetOrdering.ParseTimestamp,
                value => KeysetOrdering.FormatTimestamp(value),
                descending,
                limit,
                cancellationToken)
            : await KeysetPages.ReadAsync(
                campaigns,
                cursor,
                CampaignListSortFields.Name,
                campaign => campaign.Name,
                campaign => campaign.Id,
                RowProjection,
                row => row.Summary.Name,
                row => row.Summary.Id,
                KeysetOrdering.ParseText,
                KeysetOrdering.FormatText,
                descending,
                limit,
                cancellationToken);

        if (page is null)
        {
            return Invalid();
        }

        var items = page.Items.Select(row => row.Summary).ToArray();
        return Result<KeysetCursorPage<CampaignSummaryView>>.Success(new KeysetCursorPage<CampaignSummaryView>(items, page.NextCursor));
    }

    private static IQueryable<Campaign> Filter(IQueryable<Campaign> campaigns, ListCampaignsQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var pattern = LikePattern.Contains(query.Q);
            campaigns = campaigns.Where(campaign => EF.Functions.ILike(campaign.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            campaigns = campaigns.Where(campaign => campaign.Status == query.Status);
        }

        return campaigns;
    }

    private static Result<KeysetCursorPage<CampaignSummaryView>> Invalid() =>
        Result<KeysetCursorPage<CampaignSummaryView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed));
}
