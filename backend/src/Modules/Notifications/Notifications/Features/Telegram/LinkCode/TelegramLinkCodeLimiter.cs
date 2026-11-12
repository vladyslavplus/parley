using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Parley.Notifications.Options;

namespace Parley.Notifications.Features.Telegram.LinkCode;

/// <summary>
/// Per-user fixed-window limit of link code requests, held in memory. It runs in the handler, because the user is known only after
/// authentication.
/// </summary>
public sealed class TelegramLinkCodeLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<Guid> _limiter;

    public TelegramLinkCodeLimiter(IOptions<TelegramOptions> options)
    {
        var settings = options.Value;
        _limiter = PartitionedRateLimiter.Create<Guid, Guid>(userId => RateLimitPartition.GetFixedWindowLimiter(
            userId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.LinkCodeRequestsPerWindow,
                Window = settings.LinkCodeWindow,
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
