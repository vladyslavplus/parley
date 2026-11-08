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
    [ProducesResponseType<CursorPage<AuditEventView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] Guid? before, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListAuditEventsQuery(before, limit), cancellationToken);
        return FromResult(result, Ok);
    }
}
