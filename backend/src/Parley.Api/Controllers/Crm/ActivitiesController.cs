using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Crm.Features.Activities.List;
using Parley.Crm.Features.Activities.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Timeline of a contact, a deal or a company.</summary>
public sealed class ActivitiesController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Activities)]
    [RequiresPermission(Permissions.ActivitiesRead)]
    [ProducesResponseType<KeysetCursorPage<ActivityView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? contactId,
        [FromQuery] Guid? dealId,
        [FromQuery] Guid? companyId,
        [FromQuery] string? type,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListActivitiesQuery(after, limit, q, contactId, dealId, companyId, type, order),
            cancellationToken);
        return FromResult(result, Ok);
    }
}
