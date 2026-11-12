using Parley.Identity.Contracts.Authorization;

namespace Parley.Insights.Features.Dashboard.Overview;

/// <summary>
/// Cache key of the dashboard aggregate. It names the organization, the user and the visibility the aggregate was computed
/// for, so a cached answer is never served to a caller with a different scope.
/// </summary>
public static class DashboardCacheKeys
{
    private const string Prefix = "insights:dashboard";

    public static string For(Guid organizationId, Guid userId, DataScopeLevel dealsLevel, DataScopeLevel tasksLevel, bool campaignsVisible) =>
        string.Join(':', Prefix, organizationId, userId, dealsLevel, tasksLevel, campaignsVisible ? "campaigns" : "no-campaigns");
}
