using Parley.Insights.Contracts.Errors;
using Parley.Insights.Contracts.Scores;

namespace Parley.Insights.Domain.Ml;

/// <summary>
/// Decisions of the scoring runs as pure functions: whether a run may start, whether a finished run is good enough to
/// swap in, and how a scoring service status maps to a stable code. The job and the API both read these, so a rule lives once.
/// </summary>
public static class ScoringRunRules
{
    /// <summary>Model name the scoring service registers for each kind. Null for a kind the service does not score.</summary>
    public static string? ModelNameOf(string kind) => kind switch
    {
        ScoreKinds.Uplift => ScoringModelNames.Uplift,
        ScoreKinds.Clv => ScoringModelNames.Clv,
        _ => null,
    };

    /// <summary>True for the kinds a scoring run produces: uplift and CLV. RFM is computed inside the platform.</summary>
    public static bool IsScoredKind(string kind) => ModelNameOf(kind) is not null;

    /// <summary>
    /// The reason a run may not start, or null when it may. Checked in this order: the ML switch,
    /// a pinned active model version, then the single active run of the kind.
    /// </summary>
    public static string? StartBlocker(bool mlEnabled, string? activeVersion, bool activeRunExists)
    {
        if (!mlEnabled)
        {
            return InsightsErrorCodes.ScoringMlDisabled;
        }

        if (string.IsNullOrWhiteSpace(activeVersion))
        {
            return InsightsErrorCodes.ScoringModelUnavailable;
        }

        return activeRunExists ? InsightsErrorCodes.ScoringRunInProgress : null;
    }

    /// <summary>
    /// True when the share of rejected items is over the limit. A run with no items is never over it. The limit is a
    /// fraction between 0 and 1; a run above it fails, so a model that rejects most contacts never swaps scores in.
    /// </summary>
    public static bool RejectedShareExceeded(int rejected, int total, decimal maximumShare)
    {
        if (total <= 0 || rejected <= 0)
        {
            return false;
        }

        return (decimal)rejected / total > maximumShare;
    }

    /// <summary>
    /// The stable code of a failed scoring call, from its HTTP status. The status of the service is the only signal
    /// the integration gets for a refused call, so the mapping is fixed here.
    /// </summary>
    public static string ErrorCodeOfStatus(int statusCode) => statusCode switch
    {
        404 => InsightsErrorCodes.ScoringModelUnavailable,
        409 => InsightsErrorCodes.ScoringModelMismatch,
        401 or 413 or 422 => InsightsErrorCodes.ScoringServiceRejected,
        _ => InsightsErrorCodes.ScoringServiceUnavailable,
    };

    /// <summary>True when a response was scored with the model and feature schema the run pinned.</summary>
    public static bool MatchesPinnedModel(string responseVersion, int responseSchema, string pinnedVersion, int pinnedSchema) =>
        string.Equals(responseVersion, pinnedVersion, StringComparison.Ordinal) && responseSchema == pinnedSchema;
}
