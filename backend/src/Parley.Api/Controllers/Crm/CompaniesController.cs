using Mediator;
using Microsoft.AspNetCore.Mvc;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Localization;
using Parley.Crm.Features.Companies.BulkAssignOwner;
using Parley.Crm.Features.Companies.BulkDelete;
using Parley.Crm.Features.Companies.Create;
using Parley.Crm.Features.Companies.Delete;
using Parley.Crm.Features.Companies.Get;
using Parley.Crm.Features.Companies.List;
using Parley.Crm.Features.Companies.Restore;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Features.Companies.Update;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;
using Parley.SharedKernel.Paging;

namespace Parley.Api.Controllers.Crm;

/// <summary>Company records: list, read, create, update, soft delete and restore, bulk owner and bulk delete.</summary>
public sealed class CompaniesController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    [HttpGet(CrmRoutes.Companies)]
    [RequiresPermission(Permissions.CompaniesRead)]
    [ProducesResponseType<KeysetCursorPage<CompanyView>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] string? after,
        [FromQuery] int? limit,
        [FromQuery] string? q,
        [FromQuery] Guid? ownerUserId,
        [FromQuery] string? sort,
        [FromQuery] string? order,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListCompaniesQuery(after, limit, q, ownerUserId, sort, order), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.CompanyById)]
    [RequiresPermission(Permissions.CompaniesRead)]
    [ProducesResponseType<CompanyView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCompanyQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.Companies)]
    [RequiresPermission(Permissions.CompaniesWrite)]
    [ProducesResponseType<CompanyView>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CompanyRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateCompanyCommand(
                request?.Name ?? string.Empty,
                request?.Domain,
                request?.Country,
                request?.Industry,
                request?.OwnerUserId,
                request?.CustomFields),
            cancellationToken);
        return FromResult(result, view => Created($"{CrmRoutes.Companies}/{view.Id:D}", view));
    }

    [HttpPut(CrmRoutes.CompanyById)]
    [RequiresPermission(Permissions.CompaniesWrite)]
    [ProducesResponseType<CompanyView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateCompanyCommand(
                id,
                request?.Version ?? default,
                request?.Name ?? string.Empty,
                request?.Domain,
                request?.Country,
                request?.Industry,
                request?.CustomFields),
            cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpDelete(CrmRoutes.CompanyById)]
    [RequiresPermission(Permissions.CompaniesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCompanyCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CrmRoutes.CompanyRestore)]
    [RequiresPermission(Permissions.CompaniesDelete)]
    [ProducesResponseType<CompanyView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RestoreCompanyCommand(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPost(CrmRoutes.CompaniesBulkOwner)]
    [RequiresPermission(Permissions.CompaniesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignOwner([FromBody] BulkOwnerRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new BulkAssignCompanyOwnerCommand(request?.Ids ?? Array.Empty<Guid>(), request?.OwnerUserId ?? Guid.Empty),
            cancellationToken);
        return FromResult(result);
    }

    [HttpPost(CrmRoutes.CompaniesBulkDelete)]
    [RequiresPermission(Permissions.CompaniesDelete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkIdsRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new BulkDeleteCompaniesCommand(request?.Ids ?? Array.Empty<Guid>()), cancellationToken);
        return FromResult(result);
    }
}
