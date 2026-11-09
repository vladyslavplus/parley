using System.Text.Json;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Domain.Activities;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Features.Deals.Shared;
using Parley.Crm.Persistence;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Deals;
using Parley.Crm.Shared.Owners;
using Parley.Crm.Shared.Text;
using Parley.Identity.Contracts.Authorization;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Persistence.Concurrency;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Crm.Features.Deals.Move;

/// <summary>
/// Moves a deal in one transaction: the board position, the stage history, the timeline entries and the outbox
/// events are written together with the deal's row-version check. Two moves of one deal from the same version
/// cannot both commit: the second one matches no row and returns 409.
/// </summary>
public sealed class MoveDealCommandHandler(
    CrmDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IDataScope dataScope,
    OwnerDirectory ownerDirectory,
    IModuleOutbox<CrmDbContext> outbox,
    TimeProvider timeProvider) : IRequestHandler<MoveDealCommand, Result<DealView>>
{
    public async ValueTask<Result<DealView>> Handle(MoveDealCommand command, CancellationToken cancellationToken)
    {
        var scope = await dataScope.ForPermissionAsync(Permissions.DealsWrite, cancellationToken);
        var deal = await DealLookup.FindTrackedAsync(dbContext, command.Id, scope, cancellationToken);
        if (deal is null)
        {
            return Result<DealView>.Failure(Error.NotFound(CrmErrorCodes.DealNotFound));
        }

        var stage = await dbContext.PipelineStages.AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == command.StageId && candidate.PipelineId == deal.PipelineId, cancellationToken);
        if (stage is null)
        {
            return Result<DealView>.Failure(CrmErrors.Field(nameof(MoveDealCommand.StageId), CrmErrorCodes.DealStageNotFound));
        }

        var now = timeProvider.GetUtcNow();
        var actorId = currentUser.RequireUserId();
        var previousStageId = deal.StageId;
        var stageChanged = previousStageId != stage.Id;
        var statusChanged = deal.Status != stage.Kind;

        await PlaceAsync(deal, stage.Id, command.Position, cancellationToken);

        deal.StageId = stage.Id;
        deal.Status = stage.Kind;
        if (stageChanged)
        {
            // The reason is set when the deal enters the lost stage and cleared when it leaves; a move within a stage keeps it.
            deal.LostReason = stage.Kind == PipelineStageKinds.Lost ? TextValues.Optional(command.LostReason) : null;
        }

        deal.ClosedAt = PipelineStageKinds.IsTerminal(stage.Kind)
            ? (statusChanged ? now.UtcDateTime : deal.ClosedAt)
            : null;

        if (stageChanged)
        {
            var eventId = Guid.CreateVersion7();
            var previousStageName = await dbContext.PipelineStages.AsNoTracking()
                .Where(candidate => candidate.Id == previousStageId)
                .Select(candidate => candidate.Name)
                .FirstOrDefaultAsync(cancellationToken);
            dbContext.DealStageHistory.Add(new DealStageHistoryEntry
            {
                OrganizationId = tenantContext.OrganizationId,
                DealId = deal.Id,
                FromStageId = previousStageId,
                ToStageId = stage.Id,
                ChangedByUserId = actorId,
                ChangedAt = now.UtcDateTime,
            });
            dbContext.Activities.Add(new Activity
            {
                OrganizationId = tenantContext.OrganizationId,
                Type = ActivityTypes.DealStageChanged,
                ContactId = deal.ContactId,
                DealId = deal.Id,
                CompanyId = deal.CompanyId,
                ActorUserId = actorId,
                Payload = JsonSerializer.Serialize(new
                {
                    fromStageId = previousStageId,
                    fromStageName = previousStageName,
                    toStageId = stage.Id,
                    toStageName = stage.Name,
                    status = stage.Kind,
                }),
                OccurredAt = now.UtcDateTime,
                SourceEventId = eventId,
            });
            outbox.Add(new DealStageChanged
            {
                EventId = eventId,
                DealId = deal.Id,
                FromStageId = previousStageId,
                ToStageId = stage.Id,
                Status = stage.Kind,
                OccurredAt = now,
            });
        }

        if (statusChanged && PipelineStageKinds.IsTerminal(stage.Kind))
        {
            var eventId = Guid.CreateVersion7();
            dbContext.Activities.Add(new Activity
            {
                OrganizationId = tenantContext.OrganizationId,
                Type = ActivityTypes.DealClosed,
                ContactId = deal.ContactId,
                DealId = deal.Id,
                CompanyId = deal.CompanyId,
                ActorUserId = actorId,
                Payload = JsonSerializer.Serialize(new { stageName = stage.Name, status = stage.Kind }),
                OccurredAt = now.UtcDateTime,
                SourceEventId = eventId,
            });
            outbox.Add(new DealClosed
            {
                EventId = eventId,
                DealId = deal.Id,
                OwnerUserId = deal.OwnerUserId,
                Status = stage.Kind,
                OccurredAt = now,
            });
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

    /// <summary>
    /// Sets the board position of the moved deal at <paramref name="requestedIndex"/> among the other live deals of the
    /// target stage. Takes the middle of the two neighbours; when they leave no room, renumbers the whole stage.
    /// </summary>
    private async Task PlaceAsync(Deal deal, Guid stageId, int requestedIndex, CancellationToken cancellationToken)
    {
        var siblings = dbContext.Deals.AsNoTracking()
            .Where(other => other.StageId == stageId && other.Id != deal.Id && other.DeletedAt == null);
        var count = await siblings.CountAsync(cancellationToken);
        var index = Math.Clamp(requestedIndex, 0, count);

        // The neighbours are the rows just before and just after the index; one statement reads both.
        var neighbours = await siblings
            .OrderBy(other => other.BoardPosition)
            .ThenBy(other => other.Id)
            .Skip(Math.Max(index - 1, 0))
            .Take(2)
            .Select(other => (long?)other.BoardPosition)
            .ToListAsync(cancellationToken);
        long? previous = index > 0 ? neighbours[0] : null;
        long? next = index > 0 ? neighbours.ElementAtOrDefault(1) : neighbours.FirstOrDefault();

        if (DealBoardPositions.Between(previous, next) is { } position)
        {
            deal.BoardPosition = position;
            return;
        }

        var ordered = await dbContext.Deals
            .Where(other => other.StageId == stageId && other.Id != deal.Id && other.DeletedAt == null)
            .OrderBy(other => other.BoardPosition)
            .ThenBy(other => other.Id)
            .ToListAsync(cancellationToken);
        ordered.Insert(index, deal);
        DealBoardPositions.Renumber(ordered);
    }
}
