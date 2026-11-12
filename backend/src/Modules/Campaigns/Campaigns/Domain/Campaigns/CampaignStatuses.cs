namespace Parley.Campaigns.Domain.Campaigns;

/// <summary>Campaign states. Stored as lowercase text; a new value is a code change.</summary>
public static class CampaignStatuses
{
    public const string Draft = "draft";

    public const string Preparing = "preparing";

    public const string Ready = "ready";

    public const string Sending = "sending";

    public const string Completed = "completed";

    public const string Cancelled = "cancelled";

    public const string Failed = "failed";

    public static IReadOnlyList<string> All { get; } = [Draft, Preparing, Ready, Sending, Completed, Cancelled, Failed];

    /// <summary>True when the value names one of the campaign states.</summary>
    public static bool IsKnown(string? value) => value is not null && All.Contains(value, StringComparer.Ordinal);
}
