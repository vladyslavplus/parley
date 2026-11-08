using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Restore;

public sealed class RestoreContactCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<RestoreContactCommand, Result<ContactView>>
{
    public async ValueTask<Result<ContactView>> Handle(RestoreContactCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsDelete, cancellationToken);
        var contact = await ContactScopeFilter.Apply(
                dbContext.Contacts.Where(row => row.Id == command.Id),
                scope, includeDeleted: true)
            .FirstOrDefaultAsync(cancellationToken);
        if (contact is null)
        {
            return Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        if (contact.DeletedAt is null)
        {
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactNotDeleted));
        }

        if (contact.EmailNormalized is not null
            && await dbContext.Contacts.AnyAsync(other => other.EmailNormalized == contact.EmailNormalized && other.Id != contact.Id && other.DeletedAt == null, cancellationToken))
        {
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactEmailTaken));
        }

        var now = timeProvider.GetUtcNow();
        contact.DeletedAt = null;
        outbox.Add(new ContactRestored { ContactId = contact.Id, OwnerUserId = contact.OwnerUserId, OccurredAt = now });
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.ContactRestored, timeProvider, CrmAuditTargetTypes.Contact, contact.Id);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueConstraintViolations.IsViolationOf(exception, CrmSchemaNames.ContactEmailIndex))
        {
            return Result<ContactView>.Failure(Error.Conflict(CrmErrorCodes.ContactEmailTaken));
        }

        var row = await ContactLookup.FindRowAsync(dbContext, contact.Id, scope, cancellationToken);
        return row is null
            ? Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound))
            : Result<ContactView>.Success(ContactViews.ToView(row));
    }
}
