using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Domain.Companies;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Sorting;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Paging;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.Companies.List;

public sealed class ListCompaniesQueryHandler(CrmDbContext dbContext, IDataScope dataScope, OwnerDirectory ownerDirectory)
    : IRequestHandler<ListCompaniesQuery, Result<KeysetCursorPage<CompanyView>>>
{
    public async ValueTask<Result<KeysetCursorPage<CompanyView>>> Handle(ListCompaniesQuery query, CancellationToken cancellationToken)
    {
        var limit = PageLimits.Resolve(query.Limit);
        var descending = SortDirections.IsDescending(query.Order);
        var sortByCreated = query.Sort == ListSortFields.Created;

        KeysetCursor? cursor = null;
        if (query.After is not null && !KeysetCursor.TryDecode(query.After, out cursor))
        {
            return Failure();
        }

        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        var companies = Filter(CompanyScopeFilter.Apply(dbContext.Companies.AsNoTracking(), scope), query);

        if (sortByCreated)
        {
            var position = cursor is null ? null : KeysetOrdering.ReadPosition(cursor, ListSortFields.Created, KeysetOrdering.ParseTimestamp);
            if (cursor is not null && position is null)
            {
                return Failure();
            }

            var ordered = KeysetOrdering.Apply(companies, company => company.CreatedAt, company => company.Id, position, descending);
            var rows = await ordered.Select(CompanyViews.RowProjection).Take(limit + 1).ToListAsync(cancellationToken);
            return await ToResultAsync(KeysetCursorPage<CompanyRow>.From(
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

        var orderedByName = KeysetOrdering.Apply(companies, company => company.NameNormalized, company => company.Id, namePosition, descending);
        var nameRows = await orderedByName.Select(CompanyViews.RowProjection).Take(limit + 1).ToListAsync(cancellationToken);
        return await ToResultAsync(KeysetCursorPage<CompanyRow>.From(
            nameRows,
            limit,
            row => KeysetOrdering.CursorFor(ListSortFields.Name, row.NameNormalized, row.Id)),
            cancellationToken);
    }

    private static IQueryable<Company> Filter(IQueryable<Company> companies, ListCompaniesQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = LikePattern.Contains(query.Search);
            companies = companies.Where(company => EF.Functions.ILike(company.NameNormalized, pattern)
                || (company.Domain != null && EF.Functions.ILike(company.Domain, pattern)));
        }

        if (query.OwnerUserId is { } ownerUserId)
        {
            companies = companies.Where(company => company.OwnerUserId == ownerUserId);
        }

        return companies;
    }

    private static string? ParseName(string key) => key;

    private static Result<KeysetCursorPage<CompanyView>> Failure() =>
        Result<KeysetCursorPage<CompanyView>>.Failure(CrmErrors.Field(nameof(ListCompaniesQuery.After), ErrorCodes.ValidationFailed));

    private async Task<Result<KeysetCursorPage<CompanyView>>> ToResultAsync(KeysetCursorPage<CompanyRow> page, CancellationToken cancellationToken)
    {
        var owners = await ownerDirectory.ReadAsync(page.Items.Select(row => row.OwnerUserId), cancellationToken);
        var items = page.Items.Select(row => CompanyViews.ToView(row, owners)).ToArray();
        return Result<KeysetCursorPage<CompanyView>>.Success(new KeysetCursorPage<CompanyView>(items, page.NextCursor));
    }
}
