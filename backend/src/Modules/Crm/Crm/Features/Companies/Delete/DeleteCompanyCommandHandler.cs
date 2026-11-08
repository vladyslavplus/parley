using Mediator;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Companies.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Companies.Delete;

public sealed class DeleteCompanyCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<DeleteCompanyCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteCompanyCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.CompaniesDelete, cancellationToken);
        var company = await CompanyLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (company is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.CompanyNotFound));
        }

        company.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.CompanyDeleted, timeProvider, CrmAuditTargetTypes.Company, company.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
