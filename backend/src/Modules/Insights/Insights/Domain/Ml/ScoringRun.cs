using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Insights.Domain.Ml;

/// <summary>One scoring run of an organization. Scores are swapped in only when a run completes.</summary>
public sealed class ScoringRun : BusinessEntity
{
    public string Kind { get; set; } = string.Empty;

    public string ModelName { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public int FeatureSchemaVersion { get; set; }

    public string Status { get; set; } = string.Empty;

    public int ContactsTotal { get; set; }

    public int ContactsScored { get; set; }

    /// <summary>Items the service refused. Counted, never scored; a run above the rejected share limit fails.</summary>
    public int ContactsRejected { get; set; }

    public string? ErrorCode { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public Guid? StartedBy { get; set; }
}
