using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Contracts.Events;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Shared;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Outbox.Publishing;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Start;

public sealed class StartCampaignCommandHandler(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IModuleOutbox<CampaignsDbContext> outbox,
    CampaignSendScheduler scheduler,
    TimeProvider timeProvider) : IRequestHandler<StartCampaignCommand, Result<CampaignView>>
{
    public async ValueTask<Result<CampaignView>> Handle(StartCampaignCommand command, CancellationToken cancellationToken)
    {
        var status = await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == command.Id)
            .Select(campaign => campaign.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        if (status != CampaignStatuses.Ready)
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignInvalidState));
        }

        var now = timeProvider.GetUtcNow();
        var userId = currentUser.RequireUserId();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // The status is the guard: a concurrent start that already moved the campaign updates no row here.
        var updated = await dbContext.Campaigns
            .Where(campaign => campaign.Id == command.Id && campaign.Status == CampaignStatuses.Ready)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(campaign => campaign.Status, CampaignStatuses.Sending)
                .SetProperty(campaign => campaign.StartedAt, now.UtcDateTime)
                .SetProperty(campaign => campaign.StartedBy, (Guid?)userId)
                .SetProperty(campaign => campaign.UpdatedAt, now.UtcDateTime), cancellationToken);
        if (updated == 0)
        {
            return Result<CampaignView>.Failure(Error.Conflict(CampaignsErrorCodes.CampaignInvalidState));
        }

        outbox.Add(new CampaignStarted { CampaignId = command.Id, StartedAt = now });
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // Queued after the commit, so the job always finds a sending campaign. A lost enqueue leaves the campaign
        // sending without a job; the send job is the only resume path.
        scheduler.Schedule(tenantContext.OrganizationId, command.Id);

        var view = await CampaignLookup.FindViewAsync(dbContext, command.Id, cancellationToken);
        return view is null
            ? Result<CampaignView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound))
            : Result<CampaignView>.Success(view);
    }
}
