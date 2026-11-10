namespace Parley.Insights.Domain.Ml;

/// <summary>A trained model version of the platform. Platform row: it has no organization and is not tenant-filtered.</summary>
public sealed class ModelVersion
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string ModelName { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public int FeatureSchemaVersion { get; set; }

    public string Provenance { get; set; } = string.Empty;

    public DateTime TrainedAt { get; set; }

    public string Metrics { get; set; } = "{}";

    public DateTime SyncedAt { get; set; }
}
