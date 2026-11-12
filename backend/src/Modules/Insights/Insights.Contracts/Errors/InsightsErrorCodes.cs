namespace Parley.Insights.Contracts.Errors;

/// <summary>Stable error codes of the insights module. Each code has a localized text in the shared resources.</summary>
public static class InsightsErrorCodes
{
    public const string MlSettingsVersionConflict = "insights.ml_settings.version_conflict";

    /// <summary>The uplift model named in the ML settings is not in the model registry as an uplift model.</summary>
    public const string MlSettingsModelUnknown = "insights.ml_settings.model_unknown";

    /// <summary>The ML switch of the organization is off: no scoring run can start.</summary>
    public const string ScoringMlDisabled = "insights.scoring.ml_disabled";

    /// <summary>A scoring run of the organization is still running.</summary>
    public const string ScoringRunInProgress = "insights.scoring.run_in_progress";

    /// <summary>The requested score kind is not one the scoring run supports.</summary>
    public const string ScoringKindUnknown = "insights.scoring.kind_unknown";

    /// <summary>No scoring run with the requested identifier.</summary>
    public const string ScoringRunNotFound = "insights.scoring.run_not_found";

    /// <summary>The scoring service is off, unreachable, unhealthy or its circuit is open. The run fails; scores stay as they were.</summary>
    public const string ScoringServiceUnavailable = "insights.scoring.service_unavailable";

    /// <summary>The scoring service refused the request for a reason the integration cannot fix at run time.</summary>
    public const string ScoringServiceRejected = "insights.scoring.service_rejected";

    /// <summary>The model to score with is not available in the service registry.</summary>
    public const string ScoringModelUnavailable = "insights.scoring.model_unavailable";

    /// <summary>The service scored with a model or feature schema other than the one the run asked for.</summary>
    public const string ScoringModelMismatch = "insights.scoring.model_mismatch";

    /// <summary>The service rejected more items than the configured share allows. The run fails.</summary>
    public const string ScoringTooManyRejected = "insights.scoring.too_many_rejected";

    /// <summary>The run failed for a reason outside the scoring service. Scores stay as they were.</summary>
    public const string ScoringRunFailed = "insights.scoring.run_failed";

    /// <summary>The run stayed queued or running past the stale window, so the sweep failed it. Its scores were removed.</summary>
    public const string ScoringRunAbandoned = "insights.scoring.run_abandoned";

    /// <summary>The run was cancelled before it finished. The run fails; scores stay as they were.</summary>
    public const string ScoringCancelled = "insights.scoring.cancelled";
}
