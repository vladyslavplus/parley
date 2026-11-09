using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Deals.Get;

public sealed class GetDealQueryHandler(CrmDbContext dbContext, IDataScope dataScope, OwnerDirectory ownerDirectory)
    : IRequestHandler<GetDealQuery, Result<DealView>>
{
    public async ValueTask<Result<DealView>> Handle(GetDealQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        var row = await DealLookup.FindRowAsync(dbContext, dataScope, query.Id, scope, cancellationToken);
        if (row is null)
        {
            return Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound));
        }

        return Result<DealView>.Success(await DealViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
