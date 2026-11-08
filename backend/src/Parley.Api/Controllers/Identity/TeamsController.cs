using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Identity.Requests;
using Parley.Api.Localization;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Teams.Create;
using Parley.Identity.Features.Teams.Delete;
using Parley.Identity.Features.Teams.Get;
using Parley.Identity.Features.Teams.List;
using Parley.Identity.Features.Teams.Shared;
using Parley.Identity.Features.Teams.Update;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Identity;

/// <summary>Team administration.</summary>
public sealed class TeamsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(IdentityRoutes.Teams)]
    [RequiresPermission(Permissions.TeamsManage)]
    [ProducesResponseType<CursorPage<TeamView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] Guid? after, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTeamsQuery(after, limit), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(IdentityRoutes.TeamById)]
    [RequiresPermission(Permissions.TeamsManage)]
    [ProducesResponseType<TeamView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTeamQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(IdentityRoutes.Teams)]
    [RequiresPermission(Permissions.TeamsManage)]
    [ProducesResponseType<TeamView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] TeamRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTeamCommand(request?.Name ?? string.Empty), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(IdentityRoutes.TeamById)]
    [RequiresPermission(Permissions.TeamsManage)]
    [ProducesResponseType<TeamView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TeamRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTeamCommand(id, request?.Name ?? string.Empty), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(IdentityRoutes.TeamById)]
    [RequiresPermission(Permissions.TeamsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTeamCommand(id), cancellationToken);
        return FromResult(result);
    }
}
