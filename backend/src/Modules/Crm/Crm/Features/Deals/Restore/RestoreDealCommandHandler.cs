using Mediator;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Owners;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Audit;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Deals.Restore;

public sealed class RestoreDealCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<RestoreDealCommand, Result<DealView>>
{
    public async ValueTask<Result<DealView>> Handle(RestoreDealCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsDelete, cancellationToken);
        var deal = await DealLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken, includeDeleted: true);
        if (deal is null)
        {
            return Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound));
        }

        if (deal.DeletedAt is null)
        {
            return Result<DealView>.Failure(Error.Conflict(CrmErrorCodes.DealNotDeleted));
        }

        deal.DeletedAt = null;
        outbox.AddAuditRecorded(currentUser.RequireUserId(), tenantContext.OrganizationId, CrmAuditActions.DealRestored, timeProvider, CrmAuditTargetTypes.Deal, deal.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        var readScope = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        var row = await DealLookup.FindRowAsync(dbContext, dataScope, deal.Id, readScope, cancellationToken);
        return row is null
            ? Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound))
            : Result<DealView>.Success(await DealViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
