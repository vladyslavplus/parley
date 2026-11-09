namespace Parley.Crm.Options;

/// <summary>
/// Task reminder settings. Bind to the <c>CrmTasks</c> section.
/// </summary>
public sealed class CrmTaskOptions
{
    public const string SectionName = "CrmTasks";

    private const string DefaultQueueName = "default";

    /// <summary>How far ahead of its due instant an open task gets its reminder.</summary>
    public TimeSpan DueSoonWindow { get; set; } = TimeSpan.FromMinutes(60);

    /// <summary>Hangfire queue the per-organization reminder jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;
}
