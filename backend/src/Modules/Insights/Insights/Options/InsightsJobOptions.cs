namespace Parley.Insights.Options;

/// <summary>Settings of the insights background jobs. Bind to the <c>Insights:Jobs</c> section.</summary>
public sealed class InsightsJobOptions
{
    public const string SectionName = "Insights:Jobs";

    private const string DefaultQueueName = "default";

    /// <summary>Hangfire queue the daily organization job is enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;
}
