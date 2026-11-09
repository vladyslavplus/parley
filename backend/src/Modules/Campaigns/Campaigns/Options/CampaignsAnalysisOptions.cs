namespace Parley.Campaigns.Options;

/// <summary>
/// Statistical constants of the review: the sample ratio alarm, the critical values of the two-sided test at alpha 0.05
/// and power 0.8, and the baseline rate the preview assumes before any conversion exists. Bind to the
/// <c>Campaigns:Analysis</c> section.
/// </summary>
public sealed class CampaignsAnalysisOptions
{
    public const string SectionName = "Campaigns:Analysis";

    private const double DefaultSrmAlarmP = 0.001;

    private const double DefaultZAlpha = 1.959964;

    private const double DefaultZBeta = 0.841621;

    private const int DefaultPreviewBaselineRateBps = 2000;

    /// <summary>A sample ratio p-value below this raises the warning on the review.</summary>
    public double SrmAlarmP { get; set; } = DefaultSrmAlarmP;

    /// <summary>Critical value for alpha 0.05, two-sided.</summary>
    public double ZAlpha { get; set; } = DefaultZAlpha;

    /// <summary>Critical value for power 0.8.</summary>
    public double ZBeta { get; set; } = DefaultZBeta;

    /// <summary>Baseline conversion rate the MDE preview assumes, in basis points.</summary>
    public int PreviewBaselineRateBps { get; set; } = DefaultPreviewBaselineRateBps;
}
