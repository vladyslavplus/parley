namespace Parley.Campaigns.Options;

/// <summary>
/// Start-up rules of the send lease. A batch is claimed under one lease, so the lease must cover the slowest plausible run
/// of that batch: the SMTP timeout of every message plus the pacing gap between two messages.
/// </summary>
public static class CampaignSendLeaseRules
{
    /// <summary>
    /// True when <c>BatchSize * (mailTimeout + 1 / RatePerSecond)</c> fits inside the lease. A slower server than this makes
    /// the tail of a batch expire before it is submitted, which the expiry sweep can only recover with another round.
    /// </summary>
    public static bool BatchFitsLease(CampaignsSendOptions options, TimeSpan mailTimeout)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.RatePerSecond <= 0 || options.BatchSize <= 0)
        {
            return false;
        }

        var seconds = (mailTimeout.TotalSeconds + 1d / options.RatePerSecond) * options.BatchSize;
        return TimeSpan.FromSeconds(seconds) <= options.LeaseDuration;
    }
}
