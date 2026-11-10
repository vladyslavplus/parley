namespace Parley.Insights.Options;

/// <summary>Storage settings of the insights module. Bind to the <c>InsightsStorage</c> section.</summary>
public sealed class InsightsStorageOptions
{
    public const string SectionName = "InsightsStorage";

    /// <summary>
    /// Applies pending insights migrations at host start, in the Development environment only. Any other environment
    /// migrates through an explicit deployment step.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
