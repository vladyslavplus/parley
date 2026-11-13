namespace Parley.Insights.Domain.Priority;

/// <summary>Stable codes of the suggested next action. The UI localizes them.</summary>
public static class PrioritySuggestedActions
{
    public const string Call = "action.call";

    public const string ReviewDeal = "action.review_deal";

    public const string WinBack = "action.win_back";

    public const string Reward = "action.reward";

    public const string Nurture = "action.nurture";

    /// <summary>The uplift model says not to contact the contact now: a caution, never an active sales action.</summary>
    public const string Hold = "action.hold";
}
