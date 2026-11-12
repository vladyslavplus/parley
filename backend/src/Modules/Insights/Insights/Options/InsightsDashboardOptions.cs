namespace Parley.Insights.Options;

/// <summary>Settings of the dashboard aggregate. Bind to the <c>Insights:Dashboard</c> section.</summary>
public sealed class InsightsDashboardOptions
{
    public const string SectionName = "Insights:Dashboard";

    public const int MinimumCacheSeconds = 30;

    public const int MaximumCacheSeconds = 60;

    /// <summary>How long a dashboard aggregate is cached, per organization and visibility scope.</summary>
    public int CacheSeconds { get; set; }
}
