using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Authorization.Scope;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Domain.CustomFields;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.CustomFields;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Pipelines;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Deals.Create;

public sealed class CreateDealCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IPermissionChecker permissionChecker,
    IModuleOutbox<CrmDbContext> outbox,
    DefaultPipelineProvisioner provisioner,
    TimeProvider timeProvider) : IRequestHandler<CreateDealCommand, Result<DealView>>
{
    public async ValueTask<Result<DealView>> Handle(CreateDealCommand command, CancellationToken cancellationToken)
    {
        var organizationId = tenantContext.OrganizationId;
        var actorId = currentUser.RequireUserId();
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsWrite, cancellationToken);

        var ownerId = command.OwnerUserId ?? actorId;
        if (!await OwnerRules.IsAllowedAsync(ownerId, scope, organizationId, permissionChecker, cancellationToken))
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(CreateDealCommand.OwnerUserId), CrmErrorCodes.DealOwnerNotAllowed));
        }

        if (command.ContactId is { } contactId
            && !await (await DealLookup.VisibleContactsAsync(dbContext, dataScope, cancellationToken)).AnyAsync(contact => contact.Id == contactId, cancellationToken))
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(CreateDealCommand.ContactId), CrmErrorCodes.DealContactNotFound));
        }

        if (command.CompanyId is { } companyId
            && !await (await DealLookup.VisibleCompaniesAsync(dbContext, dataScope, cancellationToken)).AnyAsync(company => company.Id == companyId, cancellationToken))
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(CreateDealCommand.CompanyId), CrmErrorCodes.DealCompanyNotFound));
        }

        await provisioner.EnsureSavedAsync(cancellationToken);
        var pipelineId = await dbContext.Pipelines.AsNoTracking()
            .Where(pipeline => pipeline.IsDefault)
            .Select(pipeline => pipeline.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (pipelineId == Guid.Empty)
        {
            return Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.PipelineNotFound));
        }

        var stage = command.StageId is { } stageId
            ? await dbContext.PipelineStages.AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == stageId && candidate.PipelineId == pipelineId, cancellationToken)
            : await dbContext.PipelineStages.AsNoTracking()
                .Where(candidate => candidate.PipelineId == pipelineId && candidate.Kind == PipelineStageKinds.Open)
                .OrderBy(candidate => candidate.Position)
                .FirstOrDefaultAsync(cancellationToken);
        if (stage is null)
        {
            return command.StageId is null
                ? Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.StageNotFound))
                : Result<DealView>.Failure(CrmErrors.Field(nameof(CreateDealCommand.StageId), CrmErrorCodes.DealStageNotFound));
        }

        var definitions = await ActiveCustomFieldDefinitions.ReadAsync(dbContext, CustomFieldEntityTypes.Deal, cancellationToken);
        var customFields = CustomFieldValues.Merge(definitions, command.CustomFields ?? CustomFieldValues.NoValues, CrmLimits.EmptyJsonObject);
        if (!customFields.IsSuccess)
        {
            return Result<DealView>.Failure(customFields.Error!);
        }

        var now = timeProvider.GetUtcNow();
        var lastPosition = await dbContext.Deals
            .Where(deal => deal.StageId == stage.Id && deal.DeletedAt == null)
            .MaxAsync(deal => (long?)deal.BoardPosition, cancellationToken);
        var deal = new Deal
        {
            OrganizationId = organizationId,
            PipelineId = pipelineId,
            StageId = stage.Id,
            Title = TextValues.Required(command.Title),
            Amount = command.Amount,
            Currency = command.Currency.ToUpperInvariant(),
            ContactId = command.ContactId,
            CompanyId = command.CompanyId,
            OwnerUserId = ownerId,
            Status = stage.Kind,
            ExpectedCloseOn = command.ExpectedCloseOn,
            ClosedAt = PipelineStageKinds.IsTerminal(stage.Kind) ? now.UtcDateTime : null,
            BoardPosition = (lastPosition ?? 0) + CrmLimits.DealBoardPositionGap,
            CustomFields = customFields.Value!,
            Source = DealSources.Manual,
        };
        dbContext.Deals.Add(deal);
        dbContext.DealStageHistory.Add(new DealStageHistoryEntry
        {
            OrganizationId = organizationId,
            DealId = deal.Id,
            FromStageId = null,
            ToStageId = stage.Id,
            ChangedByUserId = actorId,
            ChangedAt = now.UtcDateTime,
        });
        outbox.Add(new DealAssigned { DealId = deal.Id, OwnerUserId = ownerId, OccurredAt = now });
        if (PipelineStageKinds.IsTerminal(stage.Kind))
        {
            outbox.Add(new DealClosed { DealId = deal.Id, OwnerUserId = ownerId, Status = stage.Kind, OccurredAt = now });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var readScope = await dataScope.ForPermissionAsync(Permissions.DealsRead, cancellationToken);
        var row = await DealLookup.FindRowAsync(dbContext, dataScope, deal.Id, readScope, cancellationToken);
        return row is null
            ? Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound))
            : Result<DealView>.Success(await DealViews.ToViewAsync(row, ownerDirectory, cancellationToken));
    }
}
