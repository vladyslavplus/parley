namespace Parley.Campaigns.Options;

/// <summary>Bounds of the share a score ranking targets, in basis points. Bind to the <c>Campaigns:Targeting</c> section.</summary>
public sealed class CampaignsTargetingOptions
{
    public const string SectionName = "Campaigns:Targeting";

    private const int DefaultMinShareBps = 100;

    private const int DefaultMaxShareBps = 10000;

    public int MinShareBps { get; set; } = DefaultMinShareBps;

    public int MaxShareBps { get; set; } = DefaultMaxShareBps;
}
