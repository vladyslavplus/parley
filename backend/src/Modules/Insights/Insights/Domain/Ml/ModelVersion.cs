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

    /// <summary>When the last successful sync saw this version. Rows older than that are history only.</summary>
    public DateTime SyncedAt { get; set; }

    /// <summary>True when the last successful sync reported the version as available; only such rows are scored with.</summary>
    public bool IsAvailable { get; set; } = true;
}
