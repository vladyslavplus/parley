using Mediator;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Search;
using Parley.Crm.Features.Search.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Search;

namespace Parley.Crm.Features.Search.Search;

/// <summary>
/// Ranks contacts and companies for the term. A record matches by full-text (<c>simple</c>
/// configuration), by substring, or by word similarity at or above the typo threshold, so a
/// transposed or missing letter still finds it. Each kind is read with one query under its own
/// data scope; the tenant boundary is the entity query filter, and no raw SQL is used.
/// </summary>
public sealed class SearchQueryHandler(CrmDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<SearchQuery, Result<SearchResponse>>
{
    private const string FullTextConfiguration = "simple";

    public async ValueTask<Result<SearchResponse>> Handle(SearchQuery query, CancellationToken cancellationToken)
    {
        var term = query.Q!.Trim();
        var lowered = term.ToLowerInvariant();
        var pattern = LikePattern.Contains(term);

        var hits = new List<SearchHit>();
        var contactScope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        if (contactScope.Level != DataScopeLevel.None)
        {
            hits.AddRange(await SearchContactsAsync(term, lowered, pattern, contactScope, cancellationToken));
        }

        var companyScope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        if (companyScope.Level != DataScopeLevel.None)
        {
            hits.AddRange(await SearchCompaniesAsync(term, lowered, pattern, companyScope, cancellationToken));
        }

        var items = hits
            .OrderByDescending(hit => hit.Score)
            .ThenBy(hit => hit.Title, StringComparer.Ordinal)
            .ThenBy(hit => hit.Id)
            .Take(CrmLimits.SearchResultMaximumCount)
            .Select(hit => new SearchResultItem(hit.Kind, hit.Id, hit.Title, hit.Subtitle))
            .ToArray();
        return Result<SearchResponse>.Success(new SearchResponse(items));
    }

    private async Task<IReadOnlyList<SearchHit>> SearchContactsAsync(
        string term,
        string lowered,
        string pattern,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        var rows = await ContactScopeFilter.Apply(dbContext.Contacts.AsNoTracking(), scope)
            .Where(contact => EF.Property<NpgsqlTsVector>(contact, CrmSchemaNames.SearchVectorProperty)
                    .Matches(EF.Functions.PlainToTsQuery(FullTextConfiguration, term))
                || EF.Functions.ILike(contact.NameNormalized, pattern)
                || (contact.EmailNormalized != null && EF.Functions.ILike(contact.EmailNormalized, pattern))
                || EF.Functions.TrigramsWordSimilarity(lowered, contact.NameNormalized) >= CrmLimits.SearchTypoSimilarityThreshold)
            .OrderByDescending(contact => EF.Functions.TrigramsWordSimilarity(lowered, contact.NameNormalized))
            .ThenBy(contact => contact.Id)
            .Take(CrmLimits.SearchResultMaximumCount)
            .Select(contact => new ContactSearchRow(
                contact.Id,
                contact.FirstName,
                contact.LastName,
                contact.Email,
                EF.Functions.TrigramsWordSimilarity(lowered, contact.NameNormalized)))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new SearchHit(
                SearchResultKinds.Contact,
                row.Id,
                string.Join(' ', new[] { row.FirstName, row.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))),
                row.Email,
                row.Score))
            .ToArray();
    }

    private async Task<IReadOnlyList<SearchHit>> SearchCompaniesAsync(
        string term,
        string lowered,
        string pattern,
        DataScope scope,
        CancellationToken cancellationToken)
    {
        var rows = await CompanyScopeFilter.Apply(dbContext.Companies.AsNoTracking(), scope)
            .Where(company => EF.Property<NpgsqlTsVector>(company, CrmSchemaNames.SearchVectorProperty)
                    .Matches(EF.Functions.PlainToTsQuery(FullTextConfiguration, term))
                || EF.Functions.ILike(company.NameNormalized, pattern)
                || (company.Domain != null && EF.Functions.ILike(company.Domain, pattern))
                || EF.Functions.TrigramsWordSimilarity(lowered, company.NameNormalized) >= CrmLimits.SearchTypoSimilarityThreshold)
            .OrderByDescending(company => EF.Functions.TrigramsWordSimilarity(lowered, company.NameNormalized))
            .ThenBy(company => company.Id)
            .Take(CrmLimits.SearchResultMaximumCount)
            .Select(company => new CompanySearchRow(
                company.Id,
                company.Name,
                company.Domain,
                EF.Functions.TrigramsWordSimilarity(lowered, company.NameNormalized)))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new SearchHit(SearchResultKinds.Company, row.Id, row.Name, row.Domain, row.Score)).ToArray();
    }
}
