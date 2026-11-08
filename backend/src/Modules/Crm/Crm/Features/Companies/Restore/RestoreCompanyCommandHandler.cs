using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Companies.Restore;

public sealed class RestoreCompanyCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<RestoreCompanyCommand, Result<CompanyView>>
{
    public async ValueTask<Result<CompanyView>> Handle(RestoreCompanyCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesDelete, cancellationToken);
        var company = await CompanyScopeFilter.Apply(
                dbContext.Companies.Where(row => row.Id == command.Id),
                scope, includeDeleted: true)
            .FirstOrDefaultAsync(cancellationToken);
        if (company is null)
        {
            return Result<CompanyView>.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound));
        }

        if (company.DeletedAt is null)
        {
            return Result<CompanyView>.Failure(Error.Conflict(CrmErrorCodes.CompanyNotDeleted));
        }

        company.DeletedAt = null;
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.CompanyRestored, timeProvider, CrmAuditTargetTypes.Company, company.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        var row = await CompanyLookup.FindRowAsync(dbContext, company.Id, scope, cancellationToken);
        return row is null
            ? Result<CompanyView>.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound))
            : Result<CompanyView>.Success(await CompanyViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
