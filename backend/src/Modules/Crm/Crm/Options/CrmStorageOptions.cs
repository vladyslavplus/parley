namespace Parley.Crm.Options;

/// <summary>
/// Storage switches for the CRM schema. Bind to the <c>CrmStorage</c> section.
/// </summary>
public sealed class CrmStorageOptions
{
    public const string SectionName = "CrmStorage";

    /// <summary>
    /// Applies pending CRM migrations when the host starts. Honored only in the Development
    /// environment; other environments migrate through a deployment step.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
