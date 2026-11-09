namespace Parley.Campaigns.Options;

/// <summary>Holdout share bounds and default, in basis points. Bind to the <c>Campaigns:Holdout</c> section.</summary>
public sealed class CampaignsHoldoutOptions
{
    public const string SectionName = "Campaigns:Holdout";

    private const int DefaultMinShareBps = 500;

    private const int DefaultMaxShareBps = 5000;

    private const int DefaultShareBpsValue = 1000;

    public int MinShareBps { get; set; } = DefaultMinShareBps;

    public int MaxShareBps { get; set; } = DefaultMaxShareBps;

    /// <summary>Holdout share a new campaign gets when the request names none.</summary>
    public int DefaultShareBps { get; set; } = DefaultShareBpsValue;
}
