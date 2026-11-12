namespace Parley.Campaigns.Domain.Sends;

/// <summary>
/// Decisions of the send pipeline as pure functions, so the rules that prevent a double send can be read and tested
/// without a database.
/// </summary>
public static class SendPolicy
{
    /// <summary>A claim whose lease ended at or before <paramref name="nowUtc"/> has an unknown outcome.</summary>
    public static bool IsLeaseExpired(DateTime? leaseExpiresAtUtc, DateTime nowUtc) =>
        leaseExpiresAtUtc is { } leaseEnd && leaseEnd <= nowUtc;

    /// <summary>
    /// Status after a connection-level failure before the server accepted the message. The claim goes back to the
    /// retry pool while attempts remain; the last attempt makes it final.
    /// </summary>
    public static string AfterNotAccepted(int attemptCount, int maxAttempts) =>
        CanRetry(attemptCount, maxAttempts) ? SendStatuses.RetryableFailed : SendStatuses.Failed;

    /// <summary>Whether a failed claim with this many attempts may be claimed again.</summary>
    public static bool CanRetry(int attemptCount, int maxAttempts) => attemptCount < maxAttempts;
}
