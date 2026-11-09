using Parley.Crm.Domain.Companies;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Authorization.Scope;

/// <summary>
/// Restricts company queries to the live records the current user may see. Unowned companies are
/// visible at organization level only; a record outside the scope is reported as not found.
/// Soft-deleted companies are excluded unless the includeDeleted flag is set (restore).
/// </summary>
public static class CompanyScopeFilter
{
    public static IQueryable<Company> Apply(IQueryable<Company> companies, DataScope scope, bool includeDeleted = false)
    {
        var rows = includeDeleted ? companies : companies.Where(company => company.DeletedAt == null);
        return scope.Level switch
        {
            DataScopeLevel.Organization => rows,
            DataScopeLevel.Own or DataScopeLevel.Team => ApplyOwners(rows, scope.OwnerUserIds),
            _ => rows.Where(_ => false),
        };
    }

    private static IQueryable<Company> ApplyOwners(IQueryable<Company> companies, IReadOnlyCollection<Guid>? ownerUserIds)
    {
        var owners = ownerUserIds ?? [];
        return companies.Where(company => company.OwnerUserId != null && owners.Contains(company.OwnerUserId.Value));
    }
}
