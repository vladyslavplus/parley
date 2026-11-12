using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Deals.Update;

public sealed class UpdateDealCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IPermissionChecker permissionChecker,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<UpdateDealCommand, Result<DealView>>
{
    public async ValueTask<Result<DealView>> Handle(UpdateDealCommand command, CancellationToken cancellationToken)
    {
        var actorId = currentUser.RequireUserId();
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsWrite, cancellationToken);
        var deal = await DealLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (deal is null)
        {
            return Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound));
        }

        var ownerId = command.OwnerUserId ?? deal.OwnerUserId;
        var ownerChanged = ownerId != deal.OwnerUserId;
        if (ownerChanged
            && !await OwnerRules.IsAllowedAsync(ownerId, scope, tenantContext.OrganizationId, permissionChecker, cancellationToken))
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(UpdateDealCommand.OwnerUserId), CrmErrorCodes.DealOwnerNotAllowed));
        }

        if (command.ContactId is { } contactId
            && !await (await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken)).AnyAsync(contact => contact.Id == contactId, cancellationToken))
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(UpdateDealCommand.ContactId), CrmErrorCodes.DealContactNotFound));
        }

        if (command.CompanyId is { } companyId
            && !await (await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken)).AnyAsync(company => company.Id == companyId, cancellationToken))
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(UpdateDealCommand.CompanyId), CrmErrorCodes.DealCompanyNotFound));
        }

        var definitions = await ActiveCustomFieldDefinitions.ReadAsync(dbContext, CustomFieldEntityTypes.Deal, cancellationToken);
        var customFields = CustomFieldValues.Merge(definitions, command.CustomFields, deal.CustomFields);
        if (!customFields.IsSuccess)
        {
            return Result<DealView>.Failure(customFields.Error!);
        }

        deal.Title = TextValues.Required(command.Title);
        deal.Amount = command.Amount;
        deal.Currency = command.Currency.ToUpperInvariant();
        deal.ContactId = command.ContactId;
        deal.CompanyId = command.CompanyId;
        deal.OwnerUserId = ownerId;
        deal.ExpectedCloseOn = command.ExpectedCloseOn;
        deal.CustomFields = customFields.Value!;
        if (ownerChanged)
        {
            outbox.Add(new DealAssigned { DealId = deal.Id, OwnerUserId = ownerId, ActorUserId = actorId, OccurredAt = timeProvider.GetUtcNow() });
        }

        // The stale-write check: the UPDATE matches the client's row version, so a changed row affects nothing.
        var entry = dbContext.Entry(deal);
        entry.Property<uint>(PersistenceConstants.XminPropertyName).OriginalValue = command.Version;
        entry.State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<DealView>.Failure(ConcurrencyErrors.ToConflictError());
        }

        var row = await DealLookup.FindRowAsync(dbContext, dataScope, deal.Id, scope, cancellationToken);
        return row is null
            ? Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound))
            : Result<DealView>.Success(await DealViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
