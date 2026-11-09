namespace Parley.Crm.Options;

/// <summary>
/// Contact export settings. Bind to the <c>CrmExports</c> section.
/// </summary>
public sealed class CrmExportOptions
{
    public const string SectionName = "CrmExports";

    private const string DefaultDirectory = "crm-exports";

    private const string DefaultQueueName = "default";

    /// <summary>Local directory the CSV files are written to. Relative paths resolve from the process working directory.</summary>
    public string Directory { get; set; } = DefaultDirectory;

    /// <summary>Hangfire queue export jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;
}
