using System.Globalization;
using System.Linq.Expressions;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Recipients;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Features.Daily;
using Parley.Insights.Features.Priority.Shared;
using Parley.Insights.Persistence;
using Parley.Insights.Shared;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Priority.List;

/// <summary>
/// Reads one keyset page of the latest priority list, scoped to what the caller may see, with the mode and the ML status of
/// the day. Contacts whose profile the CRM no longer returns (deleted since the build) are left out of the page. Before the
/// first complete day the page is empty and its status is the current one.
/// </summary>
public sealed class ListPriorityQueryHandler(
    InsightsDbContext dbContext,
    IDataScope dataScope,
    ICrmRecipientProfileReader profiles,
    IPermissionChecker permissions,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IRequestHandler<ListPriorityQuery, Result<PriorityListPageView>>
{
    /// <summary>The cursor orders by rank, never by value: a cursor that carried the value would hand it to every caller.</summary>
    private const string RankSortField = "rank";

    private static readonly Expression<Func<PriorityItem, PriorityItemRow>> Projection = item => new PriorityItemRow(
        item.Id,
        item.ContactId,
        item.OwnerUserId,
        item.Rank,
        item.PriorityValue,
        item.Mode,
        item.Segment,
        item.ReasonCodes,
        item.SuggestedAction);

    public async ValueTask<Result<PriorityListPageView>> Handle(ListPriorityQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Invalid();
        }

        var day = await DailyRunStamps.LatestPriorityDayAsync(dbContext, cancellationToken);
        if (day is null)
        {
            return Empty(await CurrentStatusAsync(cancellationToken));
        }

        var stamp = await dbContext.DailyRuns.AsNoTracking()
            .Where(run => run.AsOf == day.Value && run.PriorityCompletedAt != null)
            .Select(run => new { run.PriorityMode, run.PriorityMlStatus })
            .FirstOrDefaultAsync(cancellationToken);
        var mode = stamp?.PriorityMode ?? PriorityModes.Rfm;
        var mlStatus = stamp?.PriorityMlStatus ?? PriorityMlStatus.Disabled;

        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var items = InsightsScopeFilter.Priority(
            dbContext.PriorityItems.AsNoTracking().Where(item => item.AsOf == day.Value),
            scope);
        if (query.Segment is { } segment)
        {
            items = items.Where(item => item.Segment == segment);
        }

        var page = await KeysetPages.ReadAsync<PriorityItem, PriorityItemRow, int?>(
            items,
            cursor,
            RankSortField,
            item => (int?)item.Rank,
            item => item.Id,
            Projection,
            row => (int?)row.Rank,
            row => row.Id,
            ParseRank,
            FormatRank,
            descending: false,
            limit,
            cancellationToken);
        if (page is null)
        {
            return Invalid();
        }

        var contactIds = page.Items.Select(row => row.ContactId).Distinct().ToList();
        var names = contactIds.Count == 0
            ? new Dictionary<Guid, CrmRecipientProfileRow>()
            : (await profiles.ReadBatchAsync(contactIds, cancellationToken)).ToDictionary(profile => profile.ContactId);
        // Scores are the ML view's business: a caller without it sees the order and the reasons, never the value.
        var showValue = currentUser.UserId is { } userId
            && await permissions.HasPermissionAsync(userId, tenantContext.OrganizationId, Permissions.InsightsMlView, cancellationToken);
        var views = page.Items
            .Where(row => names.ContainsKey(row.ContactId))
            .Select(row => ToView(row, names[row.ContactId], showValue))
            .ToArray();
        var provenance = mode == PriorityModes.Ml
            ? await PriorityMlStatusResolver.ReadActiveUpliftProvenanceAsync(dbContext, cancellationToken)
            : null;
        return Result<PriorityListPageView>.Success(new PriorityListPageView(views, page.NextCursor, mode, mlStatus, provenance));
    }

    /// <summary>
    /// The status before the first complete day. No rows exist yet, so the list is RFM and cannot be ready: a ready switch
    /// reads as no scores until the first build has ranked a contact by uplift.
    /// </summary>
    private async Task<(string Mode, string MlStatus)> CurrentStatusAsync(CancellationToken cancellationToken)
    {
        var status = await PriorityMlStatusResolver.ReadAsync(dbContext, cancellationToken);
        return (PriorityModes.Rfm, status == PriorityMlStatus.Ready ? PriorityMlStatus.NoScores : status);
    }

    private static PriorityItemView ToView(PriorityItemRow row, CrmRecipientProfileRow profile, bool showValue) => new(
        row.ContactId,
        profile.LastName is null ? profile.FirstName : $"{profile.FirstName} {profile.LastName}",
        profile.CompanyName,
        row.OwnerUserId,
        row.Rank,
        showValue ? row.Value : null,
        row.Mode,
        row.Segment,
        row.ReasonCodes,
        row.SuggestedAction);

    private static int? ParseRank(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var rank) ? rank : null;

    private static string FormatRank(int? rank) => rank?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static Result<PriorityListPageView> Empty((string Mode, string MlStatus) status) =>
        Result<PriorityListPageView>.Success(new PriorityListPageView([], null, status.Mode, status.MlStatus, null));

    private static Result<PriorityListPageView> Invalid() =>
        Result<PriorityListPageView>.Failure(Error.Validation(ErrorCodes.ValidationFailed));
}
