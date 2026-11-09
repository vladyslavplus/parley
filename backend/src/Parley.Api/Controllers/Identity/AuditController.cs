using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Features.Audit.List;
using Parley.Identity.Features.Audit.Shared;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Identity;

/// <summary>Read-only audit log of the current organization.</summary>
public sealed class AuditController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(IdentityRoutes.AuditEvents)]
    [RequiresPermission(Permissions.AuditRead)]
    [ProducesResponseType<KeysetCursorPage<AuditEventView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] string? action,
        [FromQuery] string? actorUserId,
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListAuditEventsQuery(after, limit, q, action, actorUserId, from, to, sort, order), cancellationToken);
        return FromResult(result, Ok);
    }
}
