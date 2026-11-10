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

namespace Parley.Insights.Features.Priority.List;

/// <summary>
/// Reads one keyset page of the latest priority list, scoped to what the caller may see. Contacts whose profile the CRM no
/// longer returns (deleted since the build) are left out of the page.
/// </summary>
public sealed class ListPriorityQueryHandler(
    InsightsDbContext dbContext,
    IDataScope dataScope,
    ICrmRecipientProfileReader profiles) : IRequestHandler<ListPriorityQuery, Result<KeysetCursorPage<PriorityItemView>>>
{
    private const string PrioritySortField = "priority";

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

    public async ValueTask<Result<KeysetCursorPage<PriorityItemView>>> Handle(ListPriorityQuery query, CancellationToken cancellationToken)
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
            return Empty();
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var items = InsightsScopeFilter.Priority(
            dbContext.PriorityItems.AsNoTracking().Where(item => item.AsOf == day.Value),
            scope);
        if (query.Segment is { } segment)
        {
            items = items.Where(item => item.Segment == segment);
        }

        var page = await KeysetPages.ReadAsync<PriorityItem, PriorityItemRow, decimal?>(
            items,
            cursor,
            PrioritySortField,
            item => item.PriorityValue,
            item => item.Id,
            Projection,
            row => row.Value,
            row => row.Id,
            ParseValue,
            FormatValue,
            descending: true,
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
        var views = page.Items
            .Where(row => names.ContainsKey(row.ContactId))
            .Select(row => ToView(row, names[row.ContactId]))
            .ToArray();
        return Result<KeysetCursorPage<PriorityItemView>>.Success(new KeysetCursorPage<PriorityItemView>(views, page.NextCursor));
    }

    private static PriorityItemView ToView(PriorityItemRow row, CrmRecipientProfileRow profile) => new(
        row.ContactId,
        profile.LastName is null ? profile.FirstName : $"{profile.FirstName} {profile.LastName}",
        profile.CompanyName,
        row.OwnerUserId,
        row.Rank,
        row.Value,
        row.Mode,
        row.Segment,
        row.ReasonCodes,
        row.SuggestedAction);

    private static decimal? ParseValue(string text) =>
        decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static string FormatValue(decimal? value) => value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static Result<KeysetCursorPage<PriorityItemView>> Empty() =>
        Result<KeysetCursorPage<PriorityItemView>>.Success(new KeysetCursorPage<PriorityItemView>([], null));

    private static Result<KeysetCursorPage<PriorityItemView>> Invalid() =>
        Result<KeysetCursorPage<PriorityItemView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed));
}
