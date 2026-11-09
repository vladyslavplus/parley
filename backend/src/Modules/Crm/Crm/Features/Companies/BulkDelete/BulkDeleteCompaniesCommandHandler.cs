using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Companies.BulkDelete;

public sealed class BulkDeleteCompaniesCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<BulkDeleteCompaniesCommand, Result>
{
    public async ValueTask<Result> Handle(BulkDeleteCompaniesCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesDelete, cancellationToken);
        var ids = command.CompanyIds.Distinct().ToList();
        var companies = await CompanyScopeFilter.Apply(dbContext.Companies.Where(company => ids.Contains(company.Id)), scope)
            .ToListAsync(cancellationToken);
        if (companies.Count != ids.Count)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound));
        }

        var now = timeProvider.GetUtcNow();
        var actorId = currentUser.RequireUserId();
        foreach (var company in companies)
        {
            company.DeletedAt = now.UtcDateTime;
            outbox.AddAuditRecorded(actorId, tenantContext.OrganizationId, CrmAuditActions.CompanyDeleted, timeProvider, CrmAuditTargetTypes.Company, company.Id);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
