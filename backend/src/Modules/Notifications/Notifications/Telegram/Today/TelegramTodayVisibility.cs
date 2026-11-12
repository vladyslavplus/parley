using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Contracts.Priority;

namespace Parley.Notifications.Telegram.Today;

/// <summary>
/// The entries of the priority list one membership may see in <c>/today</c>. Own scope sees its owner's entries, team scope
/// sees the entries of the team's owners, organization scope sees all, and no scope sees nothing.
/// </summary>
public static class TelegramTodayVisibility
{
    public static IReadOnlyList<PriorityEntry> SelectVisible(DataScope scope, Guid userId, IReadOnlyList<PriorityEntry> pool, int limit)
    {
        var owners = scope.OwnerUserIds ?? [];
        IEnumerable<PriorityEntry> visible = scope.Level switch
        {
            DataScopeLevel.None => [],
            DataScopeLevel.Own => pool.Where(entry => entry.OwnerUserId == userId),
            DataScopeLevel.Team => pool.Where(entry => entry.OwnerUserId is { } owner && owners.Contains(owner)),
            _ => pool,
        };
        return visible.Take(limit).ToArray();
    }
}
