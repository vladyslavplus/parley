using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Shared;
using Parley.Crm.Contracts.Recipients;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Recipients;

/// <summary>
/// Reads one keyset page of the recipients of a campaign in freeze order. A page is empty until the audience is frozen,
/// so a partial freeze never shows. Recipients are read in batches by id; the send status, the outcome, the contact name
/// and the search are applied to each batch. The scan stops once the page and one extra match are found, or once
/// <see cref="CampaignsLimits.RecipientScanCap"/> rows were scanned. In the second case the cursor is the scan position, so
/// the client asks for the next part and no row is skipped or repeated.
/// </summary>
public sealed class ListCampaignRecipientsQueryHandler(
    CampaignsDbContext dbContext,
    ICrmRecipientProfileReader profiles,
    IDataScope dataScope) : IRequestHandler<ListCampaignRecipientsQuery, Result<KeysetCursorPage<CampaignRecipientView>>>
{
    /// <summary>Recipients read per batch. Within the CRM profile batch maximum, so one profile read covers a batch.</summary>
    private const int ScanBatchSize = 200;

    private const string IdSortField = "id";

    public async ValueTask<Result<KeysetCursorPage<CampaignRecipientView>>> Handle(ListCampaignRecipientsQuery query, CancellationToken cancellationToken)
    {
        if (!IsSupported(query))
        {
            return Invalid();
        }

        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);
        Guid? position = null;
        if (query.After is not null)
        {
            if (!KeysetCursor.TryDecode(query.After, out var cursor) || cursor is null || cursor.SortKey != IdSortField)
            {
                return Invalid();
            }

            position = cursor.Id;
        }

        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .Where(row => row.Id == query.CampaignId)
            .Select(row => new { row.AudienceFrozenAt })
            .FirstOrDefaultAsync(cancellationToken);
        if (campaign is null)
        {
            return Result<KeysetCursorPage<CampaignRecipientView>>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        if (campaign.AudienceFrozenAt is null)
        {
            return Result<KeysetCursorPage<CampaignRecipientView>>.Success(new KeysetCursorPage<CampaignRecipientView>([], null));
        }

        // Names, conversions and revenue follow the contacts scope; the address follows the organization-level scope only.
        var contactScope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var emailVisible = contactScope.Level == DataScopeLevel.Organization;
        IReadOnlyCollection<Guid>? visibleOwners = contactScope.Level == DataScopeLevel.Organization
            ? null
            : contactScope.OwnerUserIds ?? Array.Empty<Guid>();
        var needle = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim();

        var matches = new List<CampaignRecipientView>();
        var scanned = 0;
        var exhausted = false;
        while (!exhausted && matches.Count <= limit && scanned < CampaignsLimits.RecipientScanCap)
        {
            var take = Math.Min(ScanBatchSize, CampaignsLimits.RecipientScanCap - scanned);
            var rows = await ReadBatchAsync(query, position, descending, take, cancellationToken);
            exhausted = rows.Count < take;
            if (rows.Count == 0)
            {
                break;
            }

            scanned += rows.Count;
            var batch = await ToViewsAsync(query.CampaignId, rows, visibleOwners, emailVisible, cancellationToken);
            foreach (var view in batch)
            {
                if (!Matches(view, query, needle))
                {
                    continue;
                }

                matches.Add(view);
                if (matches.Count > limit)
                {
                    break;
                }
            }

            // The scan position is the last row read, so a batch cut short by the page never skips a row.
            position = rows[^1].Id;
        }

        var hasMore = matches.Count > limit;
        var items = matches.Take(limit).ToArray();
        string? nextCursor = null;
        if (hasMore)
        {
            nextCursor = new KeysetCursor(IdSortField, items[^1].RecipientId).Encode();
        }
        else if (!exhausted && position is { } scanPosition)
        {
            // The scan cap stopped the walk before the end: the cursor resumes at the scan position.
            nextCursor = new KeysetCursor(IdSortField, scanPosition).Encode();
        }

        return Result<KeysetCursorPage<CampaignRecipientView>>.Success(new KeysetCursorPage<CampaignRecipientView>(items, nextCursor));
    }

    private static bool IsSupported(ListCampaignRecipientsQuery query) =>
        (query.Sort is null || query.Sort == IdSortField)
        && (query.Arm is null || query.Arm == HoldoutArms.Treatment || query.Arm == HoldoutArms.Holdout)
        && (query.SendStatus is null || SendStatuses.All.Contains(query.SendStatus));

    /// <summary>The next batch of recipient rows after the position, in the requested direction.</summary>
    private async Task<List<RecipientRow>> ReadBatchAsync(
        ListCampaignRecipientsQuery query,
        Guid? position,
        bool descending,
        int take,
        CancellationToken cancellationToken)
    {
        var recipients = dbContext.Recipients.AsNoTracking().Where(row => row.CampaignId == query.CampaignId);
        if (query.Arm is not null)
        {
            recipients = recipients.Where(row => row.Arm == query.Arm);
        }

        if (query.Targeted is { } targeted)
        {
            recipients = recipients.Where(row => row.IsTargeted == targeted);
        }

        if (position is { } after)
        {
            recipients = descending
                ? recipients.Where(row => row.Id < after)
                : recipients.Where(row => row.Id > after);
        }

        recipients = descending
            ? recipients.OrderByDescending(row => row.Id)
            : recipients.OrderBy(row => row.Id);

        return await recipients
            .Select(row => new RecipientRow(row.Id, row.ContactId, row.Arm, row.IsTargeted, row.EmailSnapshot))
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<CampaignRecipientView>> ToViewsAsync(
        Guid campaignId,
        IReadOnlyList<RecipientRow> rows,
        IReadOnlyCollection<Guid>? visibleOwners,
        bool emailVisible,
        CancellationToken cancellationToken)
    {
        var ids = rows.Select(row => row.Id).ToArray();
        var sendStatuses = await dbContext.Sends
            .AsNoTracking()
            .Where(send => send.CampaignId == campaignId && ids.Contains(send.RecipientId))
            .Select(send => new SendRow(send.RecipientId, send.Status))
            .ToDictionaryAsync(send => send.RecipientId, cancellationToken);
        var outcomes = await dbContext.RecipientOutcomes
            .AsNoTracking()
            .Where(outcome => outcome.CampaignId == campaignId && ids.Contains(outcome.RecipientId))
            .Select(outcome => new OutcomeRow(outcome.RecipientId, outcome.Converted, outcome.FirstConversionAt, outcome.Revenue))
            .ToDictionaryAsync(outcome => outcome.RecipientId, cancellationToken);
        var names = (await profiles.ReadVisibleBatchAsync(rows.Select(row => row.ContactId).Distinct().ToArray(), visibleOwners, cancellationToken))
            .ToDictionary(profile => profile.ContactId);

        return rows.Select(row =>
        {
            // A contact missing from the names is outside the caller's scope or no longer exists: nothing about it is shown.
            var nameVisible = names.TryGetValue(row.ContactId, out var profile);
            var deleted = nameVisible && profile!.IsDeleted;
            sendStatuses.TryGetValue(row.Id, out var send);
            outcomes.TryGetValue(row.Id, out var outcome);
            if (!nameVisible || deleted)
            {
                outcome = null;
            }

            return new CampaignRecipientView(
                row.Id,
                row.ContactId,
                nameVisible ? DisplayName(profile!) : null,
                nameVisible,
                row.Arm,
                row.IsTargeted,
                send?.Status,
                !nameVisible
                    ? CampaignRecipientConversions.NotVisible
                    : deleted ? CampaignRecipientConversions.Deleted : ConversionOf(row.IsTargeted, outcome),
                outcome?.FirstConversionAt,
                outcome?.Revenue,
                emailVisible,
                emailVisible ? row.EmailSnapshot : null);
        }).ToList();
    }

    private static string ConversionOf(bool isTargeted, OutcomeRow? outcome) => outcome switch
    {
        { Converted: true } => CampaignRecipientConversions.Converted,
        { Converted: false } => CampaignRecipientConversions.NotConverted,
        _ => isTargeted ? CampaignRecipientConversions.Pending : CampaignRecipientConversions.NotTargeted,
    };

    private static string DisplayName(CrmRecipientProfileRow profile) =>
        string.IsNullOrWhiteSpace(profile.LastName) ? profile.FirstName : $"{profile.FirstName} {profile.LastName}";

    private static bool Matches(CampaignRecipientView view, ListCampaignRecipientsQuery query, string? needle)
    {
        if (query.SendStatus is not null && view.SendStatus != query.SendStatus)
        {
            return false;
        }

        if (query.Converted is { } converted
            && view.Conversion != (converted ? CampaignRecipientConversions.Converted : CampaignRecipientConversions.NotConverted))
        {
            return false;
        }

        return needle is null
            || (view.ContactName is not null && view.ContactName.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    private static Result<KeysetCursorPage<CampaignRecipientView>> Invalid() =>
        Result<KeysetCursorPage<CampaignRecipientView>>.Failure(Error.Validation(ErrorCodes.ValidationFailed));

    private sealed record RecipientRow(Guid Id, Guid ContactId, string Arm, bool IsTargeted, string? EmailSnapshot);

    private sealed record SendRow(Guid RecipientId, string Status);

    private sealed record OutcomeRow(Guid RecipientId, bool Converted, DateTime? FirstConversionAt, decimal Revenue);
}
