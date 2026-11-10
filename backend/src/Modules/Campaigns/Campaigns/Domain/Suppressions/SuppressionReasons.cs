namespace Parley.Campaigns.Domain.Suppressions;

/// <summary>Why an address is suppressed.</summary>
public static class SuppressionReasons
{
    public const string Unsubscribed = "unsubscribed";

    public const string Manual = "manual";

    public const string HardBounce = "hard_bounce";
}
