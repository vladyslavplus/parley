using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Insights.Features.Dashboard.Overview;

/// <summary>The dashboard aggregate: open pipeline, tasks due and the latest campaign verdict, limited to the caller's scope.</summary>
[RequiresPermission(Permissions.DashboardsView)]
public sealed record GetDashboardOverviewQuery : IRequest<Result<DashboardOverviewView>>;
