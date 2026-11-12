using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Sorting;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Rfm;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.List;

public sealed class ListContactsQueryHandler(
    CrmDbContext dbContext,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IRfmSegmentReader rfmSegments)
    : IRequestHandler<ListContactsQuery, Result<KeysetCursorPage<ContactView>>>
{
    public async ValueTask<Result<KeysetCursorPage<ContactView>>> Handle(ListContactsQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);
        var sortByCreated = query.Sort == ListSortFields.Created;

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var filter = new ContactListFilter(query.Search, query.TagId, query.CompanyId, query.OwnerUserId, query.CreatedFrom, query.CreatedTo);
        var contacts = ContactListFilters.Apply(ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking(), scope), filter);
        if (query.Segment is not null)
        {
            // The bound counts only the contacts the caller may see: an organization-wide segment above the cap does not fail a
            // representative who owns a few of its contacts. The list query then filters by the ids in the same statement.
            IReadOnlyCollection<Guid>? visibleOwners = scope.Level == DataScopeLevel.Organization
                ? null
                : scope.OwnerUserIds ?? Array.Empty<Guid>();
            var members = await rfmSegments.ReadMemberIdsAsync(
                query.Segment,
                CrmLimits.ContactSegmentFilterMaximum,
                visibleOwners,
                cancellationToken);
            if (members.Truncated)
            {
                return SegmentTooLarge();
            }

            var memberIds = members.ContactIds.ToArray();
            contacts = contacts.Where(contact => memberIds.Contains(contact.Id));
        }

        var companyScope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        var visibleCompanies = CompanyScopeFilter.Apply(dbContext.Companies.AsNoTracking(), companyScope);

        if (sortByCreated)
        {
            var position = cursor is null ? null : KeysetOrdering.ReadPosition(cursor, ListSortFields.Created, KeysetOrdering.ParseTimestamp);
            if (cursor is not null && position is null)
            {
                return Failure();
            }

            var ordered = KeysetOrdering.Apply(contacts, contact => contact.CreatedAt, contact => contact.Id, position, descending);
            var rows = await ordered.Select(ContactViews.RowProjectionFor(visibleCompanies)).Take(limit + 1).ToListAsync(cancellationToken);
            return await ToResultAsync(KeysetCursorPage<ContactRow>.From(
                rows,
                limit,
                row => KeysetOrdering.CursorFor(ListSortFields.Created, KeysetOrdering.FormatTimestamp(row.CreatedAt), row.Id)),
                cancellationToken);
        }

        var namePosition = cursor is null ? null : KeysetOrdering.ReadPosition(cursor, ListSortFields.Name, ParseName);
        if (cursor is not null && namePosition is null)
        {
            return Failure();
        }

        var orderedByName = KeysetOrdering.Apply(contacts, contact => contact.NameNormalized, contact => contact.Id, namePosition, descending);
        var nameRows = await orderedByName.Select(ContactViews.RowProjectionFor(visibleCompanies)).Take(limit + 1).ToListAsync(cancellationToken);
        return await ToResultAsync(KeysetCursorPage<ContactRow>.From(
            nameRows,
            limit,
            row => KeysetOrdering.CursorFor(ListSortFields.Name, row.NameNormalized, row.Id)),
            cancellationToken);
    }

    private static string? ParseName(string key) => key;

    private static Result<KeysetCursorPage<ContactView>> Failure() =>
        Result<KeysetCursorPage<ContactView>>.Failure(CrmErrors.Field(nameof(ListContactsQuery.After), ErrorCodes.ValidationFailed));

    private static Result<KeysetCursorPage<ContactView>> SegmentTooLarge() =>
        Result<KeysetCursorPage<ContactView>>.Failure(CrmErrors.Field(nameof(ListContactsQuery.Segment), CrmErrorCodes.ContactSegmentTooLarge));

    private async Task<Result<KeysetCursorPage<ContactView>>> ToResultAsync(KeysetCursorPage<ContactRow> page, CancellationToken cancellationToken)
    {
        var owners = await ownerDirectory.ReadAsync(page.Items.Select(row => row.OwnerUserId), cancellationToken);
        // One segment read per page, whatever the page size.
        var segments = await rfmSegments.ReadSegmentsAsync(page.Items.Select(row => row.Id).ToArray(), cancellationToken);
        var items = page.Items.Select(row => ContactViews.ToView(row, owners, segments.GetValueOrDefault(row.Id))).ToArray();
        return Result<KeysetCursorPage<ContactView>>.Success(new KeysetCursorPage<ContactView>(items, page.NextCursor));
    }
}
