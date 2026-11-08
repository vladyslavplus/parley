using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Tags.Create;
using Parley.Crm.Features.Tags.Delete;
using Parley.Crm.Features.Tags.List;
using Parley.Crm.Features.Tags.Shared;
using Parley.Crm.Features.Tags.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Tags of the organization: list, create, rename with color, and delete.</summary>
public sealed class TagsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Tags)]
    [RequiresPermission(Permissions.ContactsRead)]
    [ProducesResponseType<KeysetCursorPage<TagView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List([FromQuery] string? after, [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListTagsQuery(after, limit), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.Tags)]
    [RequiresPermission(Permissions.TagsManage)]
    [ProducesResponseType<TagView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] TagRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTagCommand(request?.Name ?? string.Empty, request?.ColorIndex ?? default), cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.Tags}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.TagById)]
    [RequiresPermission(Permissions.TagsManage)]
    [ProducesResponseType<TagView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] TagRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTagCommand(id, request?.Name ?? string.Empty, request?.ColorIndex ?? default), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.TagById)]
    [RequiresPermission(Permissions.TagsManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteTagCommand(id), cancellationToken);
        return FromResult(result);
    }
}
