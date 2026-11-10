namespace Parley.Campaigns.Shared;

/// <summary>
/// Input, column and template sandbox limits of the campaigns module. Validators, column mappings and the
/// template renderer read the same values.
/// </summary>
public static class CampaignsLimits
{
    public const int NameMaximumLength = 200;

    public const int LocaleMaximumLength = 10;

    public const int SubjectMaximumLength = 300;

    /// <summary>Longest template body, in characters, before parsing.</summary>
    public const int TemplateBodyMaximumLength = 100_000;

    /// <summary>Deepest nesting of template scopes (blocks inside blocks).</summary>
    public const int TemplateMaximumRecursion = 20;

    /// <summary>Most statements one render executes.</summary>
    public const int TemplateMaximumSteps = 10_000;

    /// <summary>Most characters one render writes.</summary>
    public const int TemplateMaximumOutputSize = 1_000_000;

    public const int SegmentNameMaximumLength = 200;

    /// <summary>Most owners, countries, tags or RFM segments one segment definition lists.</summary>
    public const int SegmentListMaximumCount = 50;

    public const int SegmentCountryCodeLength = 2;

    public const int SegmentRfmSegmentMaximumLength = 32;

    /// <summary>Largest recency filter, in days.</summary>
    public const int SegmentDaysSinceLastOrderMaximum = 3650;

    /// <summary>Contact identifiers returned as the sample of a segment preview.</summary>
    public const int SegmentPreviewSampleSize = 20;

    /// <summary>
    /// Most recipient rows one recipients-list request scans, whatever the filters match. A filter that matches few rows
    /// returns the items found so far with a cursor at the scan position, so the client asks for the next part.
    /// </summary>
    public const int RecipientScanCap = 1000;

    public const int StatusMaximumLength = 32;

    public const int HoldoutSeedMaximumLength = 64;

    public const int EmailMaximumLength = 320;

    /// <summary>Longest deterministic message id: two guids, a separator, and a domain of at most 253 characters.</summary>
    public const int MessageIdMaximumLength = 400;

    /// <summary>Longest stored error code.</summary>
    public const int ErrorCodeMaximumLength = 100;
}
