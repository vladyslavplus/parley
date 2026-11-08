using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Tags;

public sealed class DetachContactTagCommandHandler(CrmDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<DetachContactTagCommand, Result>
{
    public async ValueTask<Result> Handle(DetachContactTagCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsWrite, cancellationToken);
        var row = await ContactLookup.FindRowAsync(dbContext, dataScope, command.ContactId, scope, cancellationToken);
        if (row is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        await dbContext.ContactTags
            .Where(link => link.ContactId == command.ContactId && link.TagId == command.TagId)
            .ExecuteDeleteAsync(cancellationToken);
        return Result.Success();
    }
}
