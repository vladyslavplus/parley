namespace Parley.Insights.Domain.Ml;

/// <summary>The status values the scoring service reports for a model in its registry.</summary>
public static class ScoringModelStatuses
{
    /// <summary>The service can score with the model; only these versions are stored in the registry.</summary>
    public const string Available = "available";
}
