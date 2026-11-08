using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Companies.BulkAssignOwner;

public sealed class BulkAssignCompanyOwnerCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    IDataScope dataScope,
    IPermissionChecker permissionChecker) : IRequestHandler<BulkAssignCompanyOwnerCommand, Result>
{
    public async ValueTask<Result> Handle(BulkAssignCompanyOwnerCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesWrite, cancellationToken);
        if (!await OwnerRules.IsAllowedAsync(command.OwnerUserId, scope, tenantContext.OrganizationId, permissionChecker, cancellationToken))
        {
            return Result.Failure(CrmErrors.Field(nameof(BulkAssignCompanyOwnerCommand.OwnerUserId), CrmErrorCodes.CompanyOwnerNotAllowed));
        }

        var ids = command.CompanyIds.Distinct().ToArray();
        var companies = await CompanyScopeFilter.Apply(dbContext.Companies.Where(company => ids.Contains(company.Id)), scope)
            .ToListAsync(cancellationToken);
        if (companies.Count != ids.Length)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound));
        }

        foreach (var company in companies)
        {
            company.OwnerUserId = command.OwnerUserId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
