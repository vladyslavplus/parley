using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Identity.Contracts.Authorization;
using Parley.Insights.Features.Dashboard.Overview;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Dashboards;

/// <summary>Dashboards: the aggregate of the organization's pipeline, tasks and latest campaign verdict.</summary>
public sealed class DashboardsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(DashboardsRoutes.Overview)]
    [RequiresPermission(Permissions.DashboardsView)]
    [ProducesResponseType<DashboardOverviewView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDashboardOverviewQuery(), cancellationToken);
        return FromResult(result, Ok);
    }
}
