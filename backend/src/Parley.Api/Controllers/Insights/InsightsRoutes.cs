namespace Parley.Api.Controllers.Insights;

/// <summary>Route templates of the insights endpoints.</summary>
public static class InsightsRoutes
{
    public const string Priority = "insights/priority";

    public const string RfmSegments = "insights/rfm/segments";

    public const string MlSettings = "insights/ml-settings";

    public const string ScoringRuns = "insights/scoring-runs";

    public const string ScoringRun = "insights/scoring-runs/{scoringRunId:guid}";

    public const string Models = "insights/models";

    public const string ModelsSync = "insights/models/sync";

    public const string ContactScores = "contacts/{contactId:guid}/scores";
}
