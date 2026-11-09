using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.CustomFields.Archive;
using Parley.Crm.Features.CustomFields.Create;
using Parley.Crm.Features.CustomFields.List;
using Parley.Crm.Features.CustomFields.Shared;
using Parley.Crm.Features.CustomFields.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Custom field definitions: list, create, update and archive. Values are edited on the records.</summary>
public sealed class CustomFieldsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.CustomFields)]
    [RequiresPermission(Permissions.ContactsRead)]
    [ProducesResponseType<KeysetCursorPage<CustomFieldDefinitionView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] string? entityType,
        [FromQuery] string? fieldType,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListCustomFieldsQuery(after, limit, q, entityType, fieldType, sort, order), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.CustomFields)]
    [RequiresPermission(Permissions.CustomFieldsManage)]
    [ProducesResponseType<CustomFieldDefinitionView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CustomFieldRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateCustomFieldCommand(
                request?.EntityType ?? string.Empty,
                request?.Key ?? string.Empty,
                request?.Label ?? string.Empty,
                request?.FieldType ?? string.Empty,
                request?.Options,
                request?.IsRequired ?? false,
                request?.Position ?? 0),
            cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.CustomFields}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.CustomFieldById)]
    [RequiresPermission(Permissions.CustomFieldsManage)]
    [ProducesResponseType<CustomFieldDefinitionView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomFieldRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCustomFieldCommand(
                id,
                request?.Label ?? string.Empty,
                request?.Options,
                request?.IsRequired ?? false,
                request?.Position ?? 0),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.CustomFieldArchive)]
    [RequiresPermission(Permissions.CustomFieldsManage)]
    [ProducesResponseType<CustomFieldDefinitionView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ArchiveCustomFieldCommand(id), cancellationToken);
        return FromResult(result, Ok);
    }
}
