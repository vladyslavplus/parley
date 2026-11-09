using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Identity.Requests;
using Parley.Api.Localization;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Members.ChangeRole;
using Parley.Identity.Features.Members.ChangeTeam;
using Parley.Identity.Features.Members.Deactivate;
using Parley.Identity.Features.Members.List;
using Parley.Identity.Features.Members.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Identity;

/// <summary>Member administration: list, role, team and deactivation.</summary>
public sealed class MembersController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(IdentityRoutes.Members)]
    [RequiresPermission(Permissions.UsersManage)]
    [ProducesResponseType<KeysetCursorPage<MemberView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] string? roleCode,
        [FromQuery] string? teamId,
        [FromQuery] string? status,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListMembersQuery(after, limit, q, roleCode, teamId, status, sort, order), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut(IdentityRoutes.MemberRole)]
    [RequiresPermission(Permissions.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeRoleRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ChangeMemberRoleCommand(id, request?.RoleCode ?? string.Empty), cancellationToken);
        return FromResult(result);
    }

    [HttpPut(IdentityRoutes.MemberTeam)]
    [RequiresPermission(Permissions.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeTeam(Guid id, [FromBody] ChangeTeamRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ChangeMemberTeamCommand(id, request?.TeamId), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(IdentityRoutes.MemberDeactivate)]
    [RequiresPermission(Permissions.UsersManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeactivateMemberCommand(id), cancellationToken);
        return FromResult(result);
    }
}
