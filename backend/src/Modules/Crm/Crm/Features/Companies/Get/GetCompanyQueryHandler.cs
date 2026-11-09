using Mediator;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;

namespace Parley.Crm.Features.Companies.Get;

public sealed class GetCompanyQueryHandler(CrmDbContext dbContext, IDataScope dataScope, OwnerDirectory ownerDirectory)
    : IRequestHandler<GetCompanyQuery, Result<CompanyView>>
{
    public async ValueTask<Result<CompanyView>> Handle(GetCompanyQuery query, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesRead, cancellationToken);
        var row = await CompanyLookup.FindRowAsync(dbContext, query.Id, scope, cancellationToken);
        return row is null
            ? Result<CompanyView>.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound))
            : Result<CompanyView>.Success(await CompanyViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
