namespace Parley.Insights.Domain.Priority;

/// <summary>
/// Why the priority list is in its mode. Reported with the list, so a user sees when the ML scores are not in use and why.
/// </summary>
public static class PriorityMlStatus
{
    /// <summary>ML is on, an uplift model is active and a completed run exists: the ML mode is used.</summary>
    public const string Ready = "ready";

    /// <summary>The organization has ML switched off: the RFM mode is used.</summary>
    public const string Disabled = "disabled";

    /// <summary>ML is on but no uplift model version is active: the RFM mode is used.</summary>
    public const string NoModel = "no_model";

    /// <summary>ML is on and a model is active, but no scoring run has completed yet: the RFM mode is used.</summary>
    public const string NoScores = "no_scores";

    /// <summary>The status a list gets from the switch, the active model and the completed runs of the organization.</summary>
    public static string Resolve(bool mlEnabled, string? activeUpliftVersion, bool hasCompletedUpliftRun)
    {
        if (!mlEnabled)
        {
            return Disabled;
        }

        if (string.IsNullOrWhiteSpace(activeUpliftVersion))
        {
            return NoModel;
        }

        return hasCompletedUpliftRun ? Ready : NoScores;
    }
}
