using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Contacts.BulkAssignOwner;
using Parley.Crm.Features.Contacts.BulkDelete;
using Parley.Crm.Features.Contacts.Create;
using Parley.Crm.Features.Contacts.Delete;
using Parley.Crm.Features.Contacts.Get;
using Parley.Crm.Features.Contacts.List;
using Parley.Crm.Features.Contacts.Restore;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Features.Contacts.Tags;
using Parley.Crm.Features.Contacts.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Contact records: list, read, create, update, soft delete and restore, bulk owner and bulk delete, and tag attachments.</summary>
public sealed class ContactsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Contacts)]
    [RequiresPermission(Permissions.ContactsRead)]
    [ProducesResponseType<KeysetCursorPage<ContactView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? tagId,
        [FromQuery] Guid? companyId,
        [FromQuery] Guid? ownerUserId,
        [FromQuery] DateOnly? createdFrom,
        [FromQuery] DateOnly? createdTo,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new ListContactsQuery(after, limit, q, tagId, companyId, ownerUserId, createdFrom, createdTo, sort, order),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.ContactById)]
    [RequiresPermission(Permissions.ContactsRead)]
    [ProducesResponseType<ContactView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetContactQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.Contacts)]
    [RequiresPermission(Permissions.ContactsWrite)]
    [ProducesResponseType<ContactView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] ContactRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateContactCommand(
                request?.FirstName ?? string.Empty,
                request?.LastName,
                request?.Email,
                request?.Phone,
                request?.JobTitle,
                request?.Country,
                request?.City,
                request?.CompanyId,
                request?.OwnerUserId,
                request?.CustomFields),
            cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.Contacts}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.ContactById)]
    [RequiresPermission(Permissions.ContactsWrite)]
    [ProducesResponseType<ContactView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateContactCommand(
                id,
                request?.Version ?? default,
                request?.FirstName ?? string.Empty,
                request?.LastName,
                request?.Email,
                request?.Phone,
                request?.JobTitle,
                request?.Country,
                request?.City,
                request?.CompanyId,
                request?.CustomFields),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.ContactById)]
    [RequiresPermission(Permissions.ContactsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteContactCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CrmRoutes.ContactRestore)]
    [RequiresPermission(Permissions.ContactsDelete)]
    [ProducesResponseType<ContactView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreContactCommand(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.ContactsBulkOwner)]
    [RequiresPermission(Permissions.ContactsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignOwner([FromBody] BulkOwnerRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new BulkAssignContactOwnerCommand(request?.Ids ?? Array.Empty<Guid>(), request?.OwnerUserId ?? Guid.Empty),
            cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CrmRoutes.ContactsBulkDelete)]
    [RequiresPermission(Permissions.ContactsDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkIdsRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new BulkDeleteContactsCommand(request?.Ids ?? Array.Empty<Guid>()), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CrmRoutes.ContactTags)]
    [RequiresPermission(Permissions.ContactsWrite)]
    [ProducesResponseType<ContactView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachTags(Guid id, [FromBody] AttachTagsRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AttachContactTagsCommand(id, request?.TagIds ?? Array.Empty<Guid>()),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.ContactTag)]
    [RequiresPermission(Permissions.ContactsWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachTag(Guid id, Guid tagId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DetachContactTagCommand(id, tagId), cancellationToken);
        return FromResult(result);
    }
}
