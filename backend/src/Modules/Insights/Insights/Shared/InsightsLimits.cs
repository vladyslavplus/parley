namespace Parley.Insights.Shared;

/// <summary>Size limits of the insights module. Mappings and validators read the same values.</summary>
public static class InsightsLimits
{
    /// <summary>Short codes: scoring kind, mode, status, segment, suggested action, provenance.</summary>
    public const int CodeMaximumLength = 32;

    /// <summary>Model names and versions.</summary>
    public const int VersionMaximumLength = 64;

    /// <summary>Rows per statement when the RFM snapshots of one organization day are written.</summary>
    public const int RfmWriteBatchSize = 500;

    /// <summary>Snapshot rows read per step when the priority list is built; the CRM fact reader takes at most this many contacts.</summary>
    public const int PriorityBatchSize = 200;

    /// <summary>Largest number of priority entries a caller can ask for at once.</summary>
    public const int MaximumTopLimit = 200;

    /// <summary>Rows per insert when the priority items of one day are written.</summary>
    public const int PriorityWriteBatchSize = 500;

    /// <summary>Text form of an organization day in job arguments.</summary>
    public const string DayFormat = "yyyy-MM-dd";
}
