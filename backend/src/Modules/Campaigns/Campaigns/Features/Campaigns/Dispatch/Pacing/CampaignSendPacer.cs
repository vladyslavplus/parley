using Microsoft.Extensions.Options;
using Parley.Campaigns.Options;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Pacing;

/// <summary>
/// Spaces the sends of this process. Each caller holds the gate for one interval before it sends, so the starts of
/// all send jobs in the process are at least one interval apart.
/// <para>
/// The limit is per process, and it is the only limit. The gate lives in this object's memory, so two API or worker
/// processes that send at the same time start up to two sends per interval. Nothing coordinates them, by design: a
/// shared limiter would need a store on the send path. Size <c>Campaigns:Send:RatePerSecond</c> for one process and
/// divide it by the number of processes that send.
/// </para>
/// </summary>
public sealed class CampaignSendPacer : IDisposable
{
    private readonly TimeSpan _interval;

    private readonly SemaphoreSlim _gate = new(1, 1);

    public CampaignSendPacer(IOptions<CampaignsSendOptions> options)
    {
        _interval = TimeSpan.FromSeconds(1) / options.Value.RatePerSecond;
    }

    public async Task WaitTurnAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await Task.Delay(_interval, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
