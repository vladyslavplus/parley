namespace Parley.Insights.Contracts.Priority;

/// <summary>
/// Stable reason codes of the priority list that other modules need. The UI localizes them; the codes never change.
/// </summary>
public static class PriorityReasonCodes
{
    /// <summary>The uplift model marks the contact do-not-disturb: the list keeps it as a caution, never as an action.</summary>
    public const string DoNotDisturb = "reason.do_not_disturb";
}
