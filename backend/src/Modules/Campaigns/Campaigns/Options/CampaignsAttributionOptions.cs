namespace Parley.Campaigns.Options;

/// <summary>Settings of the attribution job and its daily sweep. Bind to the <c>Campaigns:Attribution</c> section.</summary>
public sealed class CampaignsAttributionOptions
{
    public const string SectionName = "Campaigns:Attribution";

    private const string DefaultQueueName = "default";

    /// <summary>Hangfire queue attribution jobs are enqueued on. Must be a queue a server listens on.</summary>
    public string QueueName { get; set; } = DefaultQueueName;
}
