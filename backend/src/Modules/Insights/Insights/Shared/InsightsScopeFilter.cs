using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Domain.Priority;
using Parley.Insights.Domain.Rfm;

namespace Parley.Insights.Shared;

/// <summary>
/// Owner filter of the insights reads. An organization-level scope sees every row, including unowned ones; a narrower
/// scope sees only rows whose owner is in its list, so unowned rows are visible at organization level only.
/// </summary>
public static class InsightsScopeFilter
{
    public static IQueryable<PriorityItem> Priority(IQueryable<PriorityItem> source, DataScope scope)
    {
        if (scope.Level == DataScopeLevel.Organization)
        {
            return source;
        }

        var owners = OwnerIds(scope);
        return source.Where(item => item.OwnerUserId != null && owners.Contains(item.OwnerUserId));
    }

    public static IQueryable<RfmSnapshot> Snapshots(IQueryable<RfmSnapshot> source, DataScope scope)
    {
        if (scope.Level == DataScopeLevel.Organization)
        {
            return source;
        }

        var owners = OwnerIds(scope);
        return source.Where(snapshot => snapshot.OwnerUserId != null && owners.Contains(snapshot.OwnerUserId));
    }

    /// <summary>The visible owners as nullable ids, the type the owner columns carry.</summary>
    private static Guid?[] OwnerIds(DataScope scope) =>
        (scope.OwnerUserIds ?? []).Select(id => (Guid?)id).ToArray();
}
