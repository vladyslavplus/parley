using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Localization;
using Parley.Crm.Contracts.Search;
using Parley.Crm.Features.Search.Search;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Crm;

/// <summary>Global search over the records the caller may see.</summary>
public sealed class SearchController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Search)]
    [RequiresPermission(Permissions.ContactsRead)]
    [ProducesResponseType<SearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SearchQuery(q), cancellationToken);
        return FromResult(result, Ok);
    }
}
