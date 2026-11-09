namespace Parley.Campaigns.Options;

/// <summary>
/// Storage switches for the campaigns schema. Bind to the <c>CampaignsStorage</c> section.
/// </summary>
public sealed class CampaignsStorageOptions
{
    public const string SectionName = "CampaignsStorage";

    /// <summary>
    /// Applies pending campaigns migrations when the host starts. Honored only in the Development
    /// environment; other environments migrate through a deployment step.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
