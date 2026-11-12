using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Features.Campaigns.TestSend;

/// <summary>
/// Per-user fixed-window limit of test sends, held in memory with the framework's partitioned rate limiter. It runs in the
/// handler, because the user is known only after authentication; the middleware that partitions by client address runs
/// before it.
/// </summary>
public sealed class CampaignTestSendLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<Guid> _limiter;

    public CampaignTestSendLimiter(IOptions<CampaignsSendOptions> options)
    {
        var settings = options.Value;
        _limiter = PartitionedRateLimiter.Create<Guid, Guid>(userId => RateLimitPartition.GetFixedWindowLimiter(
            userId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.TestSendPermitLimit,
                Window = settings.TestSendWindow,
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
    }

    /// <summary>Takes one permit for the user. False when the user has used the window's permits.</summary>
    public bool TryAcquire(Guid userId)
    {
        using var lease = _limiter.AttemptAcquire(userId, 1);
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
