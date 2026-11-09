using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Crm.Features.Owners.List;
using Parley.Crm.Features.Owners.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Crm;

/// <summary>Owner lookup for the owner pickers and filters: the people the caller may assign records to.</summary>
public sealed class OwnersController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Owners)]
    [RequiresPermission(Permissions.ContactsRead)]
    [ProducesResponseType<IReadOnlyList<OwnerView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListOwnersQuery(q, limit), cancellationToken);
        return FromResult(result, Ok);
    }
}
