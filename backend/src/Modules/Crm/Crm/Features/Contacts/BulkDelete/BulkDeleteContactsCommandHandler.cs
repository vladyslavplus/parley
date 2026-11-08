using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.BulkDelete;

public sealed class BulkDeleteContactsCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<BulkDeleteContactsCommand, Result>
{
    public async ValueTask<Result> Handle(BulkDeleteContactsCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsDelete, cancellationToken);
        var ids = command.ContactIds.Distinct().ToArray();
        var contacts = await ContactScopeFilter.Apply(dbContext.Contacts.Where(contact => ids.Contains(contact.Id)), scope)
            .ToListAsync(cancellationToken);
        if (contacts.Count != ids.Length)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        var now = timeProvider.GetUtcNow();
        var actorId = currentUser.RequireUserId();
        foreach (var contact in contacts)
        {
            contact.DeletedAt = now.UtcDateTime;
            outbox.Add(new ContactDeleted { ContactId = contact.Id, OwnerUserId = contact.OwnerUserId, OccurredAt = now });
            outbox.AddAuditRecorded(actorId, tenantContext.OrganizationId, CrmAuditActions.ContactDeleted, timeProvider, CrmAuditTargetTypes.Contact, contact.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
