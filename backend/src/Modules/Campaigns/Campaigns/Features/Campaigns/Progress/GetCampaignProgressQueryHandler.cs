using Mediator;
using Microsoft.EntityFrameworkCore;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Persistence;
using Parley.SharedKernel.Results;

namespace Parley.Campaigns.Features.Campaigns.Progress;

public sealed class GetCampaignProgressQueryHandler(CampaignsDbContext dbContext)
    : IRequestHandler<GetCampaignProgressQuery, Result<CampaignProgressView>>
{
    public async ValueTask<Result<CampaignProgressView>> Handle(GetCampaignProgressQuery query, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Campaigns
            .AsNoTracking()
            .AnyAsync(campaign => campaign.Id == query.Id, cancellationToken);
        if (!exists)
        {
            return Result<CampaignProgressView>.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        var total = await dbContext.Recipients
            .AsNoTracking()
            .CountAsync(recipient => recipient.CampaignId == query.Id
                && recipient.Arm == HoldoutArms.Treatment
                && recipient.IsTargeted, cancellationToken);

        var byStatus = await dbContext.Sends
            .AsNoTracking()
            .Where(send => send.CampaignId == query.Id)
            .GroupBy(send => send.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.Status, row => row.Count, cancellationToken);

        var sent = Count(byStatus, SendStatuses.Sent);
        var failed = Count(byStatus, SendStatuses.Failed);
        var unknown = Count(byStatus, SendStatuses.Unknown);
        var suppressed = Count(byStatus, SendStatuses.SkippedSuppressed);
        var queued = Math.Max(0, total - sent - failed - unknown - suppressed);

        return Result<CampaignProgressView>.Success(new CampaignProgressView(total, queued, sent, failed, unknown, suppressed));
    }

    private static int Count(IReadOnlyDictionary<string, int> counts, string status) =>
        counts.TryGetValue(status, out var count) ? count : 0;
}
