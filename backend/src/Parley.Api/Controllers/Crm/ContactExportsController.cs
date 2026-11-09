using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Parley.Api.Controllers.Common;
using Parley.Api.Controllers.Crm.Requests;
using Parley.Api.Idempotency;
using Parley.Api.Localization;
using Parley.Crm.Features.Contacts.Exports.Download;
using Parley.Crm.Features.Contacts.Exports.Get;
using Parley.Crm.Features.Contacts.Exports.Request;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Authorization;

namespace Parley.Api.Controllers.Crm;

/// <summary>Contact CSV exports: request with an idempotency key, poll the status, download while the link is valid.</summary>
public sealed class ContactExportsController(IMediator mediator, ErrorTextLocalizer errorTexts) : ApiControllerBase(errorTexts)
{
    private const string CsvContentType = "text/csv; charset=utf-8";

    [HttpPost(CrmRoutes.ContactExports)]
    [CrmIdempotencyKey]
    [RequiresPermission(Permissions.ContactsExport)]
    [ProducesResponseType<ContactExportView>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RequestExport(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RequestContactExportRequest? request,
        CancellationToken cancellationToken)
    {
        var filter = new ContactListFilter(
            request?.Search,
            request?.TagId,
            request?.CompanyId,
            request?.OwnerUserId,
            request?.CreatedFrom,
            request?.CreatedTo);
        var result = await mediator.Send(new RequestContactExportCommand(filter), cancellationToken);
        return FromResult(result, view => Accepted(ExportLocation(view.Id), view));
    }

    [HttpGet(CrmRoutes.ContactExportById)]
    [RequiresPermission(Permissions.ContactsExport)]
    [ProducesResponseType<ContactExportView>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetContactExportQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet(CrmRoutes.ContactExportDownload)]
    [RequiresPermission(Permissions.ContactsExport)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DownloadContactExportQuery(id), cancellationToken);
        return FromResult(result, file => PhysicalFile(file.Path, CsvContentType, file.DownloadName));
    }

    private static string ExportLocation(Guid id) => $"{CrmRoutes.ContactExports}/{id:D}";
}
