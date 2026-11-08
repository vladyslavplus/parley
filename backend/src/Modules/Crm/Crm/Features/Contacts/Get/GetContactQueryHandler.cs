using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Contacts.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Contacts.Get;

public sealed class GetContactQueryHandler(CrmDbContext dbContext, IDataScope dataScope)
    : IRequestHandler<GetContactQuery, Result<ContactView>>
{
    public async ValueTask<Result<ContactView>> Handle(GetContactQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.ContactsRead, cancellationToken);
        var row = await ContactLookup.FindRowAsync(dbContext, query.Id, scope, cancellationToken);
        if (row is null)
        {
            return Result<ContactView>.Failure(Error.NotFound(CrmErrorCodes.ContactNotFound));
        }

        return Result<ContactView>.Success(ContactViews.ToView(row));
    }
}
