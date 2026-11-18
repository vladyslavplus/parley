using Parley.Insights.Contracts.Priority;

namespace Parley.Insights.Domain.Priority;

/// <summary>Stable reason codes of the priority list. The UI localizes them; the codes never change.</summary>
public static class PriorityReasons
{
    public const string HighValue = "reason.high_value";

    public const string RecentOrder = "reason.recent_order";

    public const string TaskDue = "reason.task_due";

    public const string DealStale = "reason.deal_stale";

    /// <summary>The uplift of the contact is at or above the configured high mark.</summary>
    public const string UpliftHigh = "reason.uplift_high";

    /// <summary>
    /// The uplift model marks the contact do-not-disturb. The contact stays on the list, ranked last, and the UI shows the
    /// code as a caution chip instead of hiding the contact. The same constant is public to other modules.
    /// </summary>
    public const string DoNotDisturb = PriorityReasonCodes.DoNotDisturb;
}
