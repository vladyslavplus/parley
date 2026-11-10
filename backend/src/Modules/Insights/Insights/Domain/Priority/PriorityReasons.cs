namespace Parley.Insights.Domain.Priority;

/// <summary>Stable reason codes of the priority list. The UI localizes them; the codes never change.</summary>
public static class PriorityReasons
{
    public const string HighValue = "reason.high_value";

    public const string RecentOrder = "reason.recent_order";

    public const string AtRisk = "reason.at_risk";

    public const string TaskDue = "reason.task_due";

    public const string DealStale = "reason.deal_stale";
}
