namespace Parley.Campaigns.Domain.Sends;

/// <summary>
/// Outcome states of one send claim. <c>claimed</c> means a worker holds the claim under a lease. Of the others,
/// only <c>retryable_failed</c> may be claimed again. Stored as lowercase text.
/// </summary>
public static class SendStatuses
{
    public const string Claimed = "claimed";

    public const string Sent = "sent";

    public const string RetryableFailed = "retryable_failed";

    public const string Failed = "failed";

    public const string SkippedSuppressed = "skipped_suppressed";

    public const string Unknown = "unknown";

    public static IReadOnlyList<string> All { get; } =
    [
        Claimed,
        Sent,
        RetryableFailed,
        Failed,
        SkippedSuppressed,
        Unknown,
    ];
}
