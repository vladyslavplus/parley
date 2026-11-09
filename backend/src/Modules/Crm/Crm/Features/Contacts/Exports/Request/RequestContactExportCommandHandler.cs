using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Exports;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Exports.Request;

public sealed class RequestContactExportCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider,
    ContactExportScheduler scheduler) : IRequestHandler<RequestContactExportCommand, Result<ContactExportView>>
{
    public async ValueTask<Result<ContactExportView>> Handle(RequestContactExportCommand command, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var requester = currentUser.RequireUserId();
        var organizationId = tenantContext.OrganizationId;
        var export = new ContactExport(
            organizationId,
            requester,
            JsonSerializer.Serialize(command.Filter),
            now.UtcDateTime.Add(CrmLimits.ContactExportLinkLifetime));

        dbContext.ContactExports.Add(export);
        outbox.AddAuditRecorded(requester, organizationId, CrmAuditActions.ContactsExported, timeProvider, CrmAuditTargetTypes.ContactExport, export.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            scheduler.Schedule(organizationId, export.Id);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The row and its audit event already committed. A row left queued would sit until it expires
            // with no job behind it, so it is marked failed and the caller gets a stable error instead.
            await dbContext.ContactExports
                .Where(row => row.Id == export.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Status, ContactExportStatuses.Failed), CancellationToken.None);
            return Result<ContactExportView>.Failure(Error.Failure(CrmErrorCodes.ContactExportEnqueueFailed));
        }

        return Result<ContactExportView>.Success(ContactExportViews.ToView(export, now.UtcDateTime));
    }
}
