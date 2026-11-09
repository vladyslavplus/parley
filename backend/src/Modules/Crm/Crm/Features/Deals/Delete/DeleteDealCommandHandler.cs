using Mediator;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Deals.Delete;

public sealed class DeleteDealCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<DeleteDealCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteDealCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsDelete, cancellationToken);
        var deal = await DealLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (deal is null)
        {
            return Result.Failure(Error.NotFound(CrmErrorCodes.DealNotFound));
        }

        deal.DeletedAt = timeProvider.GetUtcNow().UtcDateTime;
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.DealDeleted, timeProvider, CrmAuditTargetTypes.Deal, deal.Id);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
