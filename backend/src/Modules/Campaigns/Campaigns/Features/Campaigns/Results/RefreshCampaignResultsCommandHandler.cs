using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Attribution;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Results;

public sealed class RefreshCampaignResultsCommandHandler(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    CampaignAttributionScheduler scheduler) : IRequestHandler<RefreshCampaignResultsCommand, Result>
{
    public async ValueTask<Result> Handle(RefreshCampaignResultsCommand command, CancellationToken cancellationToken)
    {
        var status = await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == command.Id)
            .Select(campaign => (string?)campaign.Status)
            .FirstOrDefaultAsync(cancellationToken);
        if (status is null)
        {
            return Result.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        // Only a completed campaign has a result to compute; a running one is still being sent.
        if (status != CampaignStatuses.Completed)
        {
            return Result.Failure(Error.Conflict(CampaignsErrorCodes.CampaignInvalidState));
        }

        scheduler.Schedule(tenantContext.OrganizationId, command.Id);
        return Result.Success();
    }
}
