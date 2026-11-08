using Hangfire.Dashboard;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Api.Hangfire;

/// <summary>
/// Guards the Hangfire dashboard: only an authenticated platform operator may
/// open it. Job storage is instance-wide and job arguments include
/// organization ids, so an organization admin would see other tenants' jobs —
/// a tenant-isolation break. Everyone else is denied, including anonymous
/// callers.
/// </summary>
public sealed class PlatformOperatorDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var claim = httpContext.User.FindFirst(TenancyClaimTypes.PlatformOperator)?.Value;
        return string.Equals(claim, TenancyClaimTypes.PlatformOperatorValue, StringComparison.OrdinalIgnoreCase);
    }
}
