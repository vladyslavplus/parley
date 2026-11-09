using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Contacts.Exports.Shared;
using Parley.Crm.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Contacts.Exports.Get;

public sealed class GetContactExportQueryHandler(
    CrmDbContext dbContext,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IRequestHandler<GetContactExportQuery, Result<ContactExportView>>
{
    public async ValueTask<Result<ContactExportView>> Handle(GetContactExportQuery query, CancellationToken cancellationToken)
    {
        var requester = currentUser.RequireUserId();
        var export = await dbContext.ContactExports.AsNoTracking()
            .FirstOrDefaultAsync(row => row.Id == query.Id && row.RequestedByUserId == requester, cancellationToken);
        if (export is null)
        {
            return Result<ContactExportView>.Failure(Error.NotFound(CrmErrorCodes.ContactExportNotFound));
        }

        return Result<ContactExportView>.Success(ContactExportViews.ToView(export, timeProvider.GetUtcNow().UtcDateTime));
    }
}
