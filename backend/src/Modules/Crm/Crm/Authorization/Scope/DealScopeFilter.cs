using Parley.Crm.Domain.Deals;
using Parley.Identity.Contracts.Authorization;

namespace Parley.Crm.Authorization.Scope;

/// <summary>
/// Restricts deal queries to the deals the current user may see. A deal is visible through its owner;
/// a deal outside the scope is reported as not found. Soft-deleted deals are excluded unless asked for.
/// </summary>
public static class DealScopeFilter
{
    public static IQueryable<Deal> Apply(IQueryable<Deal> deals, DataScope scope, bool includeDeleted = false)
    {
        var rows = includeDeleted ? deals : deals.Where(deal => deal.DeletedAt == null);
        return scope.Level switch
        {
            DataScopeLevel.Organization => rows,
            DataScopeLevel.Own or DataScopeLevel.Team => ApplyOwners(rows, scope.OwnerUserIds),
            _ => rows.Where(_ => false),
        };
    }

    private static IQueryable<Deal> ApplyOwners(IQueryable<Deal> deals, IReadOnlyCollection<Guid>? ownerUserIds)
    {
        var owners = ownerUserIds?.ToList() ?? [];
        return deals.Where(deal => owners.Contains(deal.OwnerUserId));
    }
}
