namespace Parley.Notifications.Contracts.Types;

/// <summary>
/// In-app notification types. Stored as text; the frontend renders the text from the type and the stored parameters, in the
/// viewer's locale.
/// </summary>
public static class NotificationTypes
{
    public const string TaskAssigned = "task_assigned";

    public const string TaskDueSoon = "task_due_soon";

    public const string DealAssigned = "deal_assigned";

    public const string CampaignCompleted = "campaign_completed";

    public const string ScoringRunCompleted = "scoring_run_completed";

    public const string MemberActivated = "member_activated";

    /// <summary>Every type a user can switch, in the order the preferences list shows them.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        TaskAssigned,
        TaskDueSoon,
        DealAssigned,
        CampaignCompleted,
        ScoringRunCompleted,
        MemberActivated,
    ];
}
