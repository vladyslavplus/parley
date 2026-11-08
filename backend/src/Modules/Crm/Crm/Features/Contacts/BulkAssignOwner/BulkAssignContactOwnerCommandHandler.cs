using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.BulkAssignOwner;

public sealed class BulkAssignContactOwnerCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    IDataScope dataScope,
    IPermissionChecker permissionChecker) : IRequestHandler<BulkAssignContactOwnerCommand, Result>
{
    public async ValueTask<Result> Handle(BulkAssignContactOwnerCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsWrite, cancellationToken);
        if (!await OwnerRules.IsAllowedAsync(command.OwnerUserId, scope, tenantContext.OrganizationId, permissionChecker, cancellationToken))
        {
            return Result.Failure(CrmErrors.Field(nameof(BulkAssignContactOwnerCommand.OwnerUserId), CrmErrorCodes.ContactOwnerNotAllowed));
        }

        var ids = command.ContactIds.Distinct().ToArray();
        var contacts = await ContactScopeFilter.Apply(dbContext.Contacts.Where(contact => ids.Contains(contact.Id)), scope)
            .ToListAsync(cancellationToken);
        if (contacts.Count != ids.Length)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        foreach (var contact in contacts)
        {
            contact.OwnerUserId = command.OwnerUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
