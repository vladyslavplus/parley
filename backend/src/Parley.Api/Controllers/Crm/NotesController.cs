using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Notes.Create;
using Parley.Crm.Features.Notes.Delete;
using Parley.Crm.Features.Notes.List;
using Parley.Crm.Features.Notes.Shared;
using Parley.Crm.Features.Notes.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Notes on a contact, a deal or a company: list one subject's notes, write, edit and soft delete.</summary>
public sealed class NotesController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Notes)]
    [RequiresPermission(Permissions.ActivitiesRead)]
    [ProducesResponseType<KeysetCursorPage<NoteView>>(StatusCodes.Status200OK)]
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
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListNotesQuery(after, limit, q, contactId, dealId, companyId, order), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.Notes)]
    [RequiresPermission(Permissions.ActivitiesWrite)]
    [ProducesResponseType<NoteView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] NoteRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateNoteCommand(request?.Body ?? string.Empty, request?.ContactId, request?.DealId, request?.CompanyId),
            cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.Notes}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.NoteById)]
    [RequiresPermission(Permissions.ActivitiesWrite)]
    [ProducesResponseType<NoteView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateNoteRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateNoteCommand(id, request?.Version ?? default, request?.Body ?? string.Empty),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.NoteById)]
    [RequiresPermission(Permissions.ActivitiesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteNoteCommand(id), cancellationToken);
        return FromResult(result);
    }
}
