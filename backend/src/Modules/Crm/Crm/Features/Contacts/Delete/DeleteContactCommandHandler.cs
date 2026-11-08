using Mediator;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Delete;

public sealed class DeleteContactCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<DeleteContactCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteContactCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsDelete, cancellationToken);
        var contact = await ContactLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (contact is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        var now = timeProvider.GetUtcNow();
        contact.DeletedAt = now.UtcDateTime;
        outbox.Add(new ContactDeleted { ContactId = contact.Id, OwnerUserId = contact.OwnerUserId, OccurredAt = now });
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.ContactDeleted, timeProvider, CrmAuditTargetTypes.Contact, contact.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
