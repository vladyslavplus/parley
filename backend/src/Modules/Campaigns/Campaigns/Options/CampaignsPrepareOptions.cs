namespace Parley.Campaigns.Options;

/// <summary>Settings of the prepare job. Bind to the <c>Campaigns:Prepare</c> section.</summary>
public sealed class CampaignsPrepareOptions
{
    public const string SectionName = "Campaigns:Prepare";

    private const string DefaultQueueName = "default";

    private const int DefaultBatchSize = 1000;

    /// <summary>Hangfire queue prepare jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;

    /// <summary>Recipients written per insert statement during the freeze.</summary>
    public int BatchSize { get; set; } = DefaultBatchSize;
}
