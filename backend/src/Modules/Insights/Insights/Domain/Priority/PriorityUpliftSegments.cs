namespace Parley.Insights.Domain.Priority;

/// <summary>Segment labels of the uplift model. They match the scoring service contract.</summary>
public static class PriorityUpliftSegments
{
    public const string Persuadable = "persuadable";

    public const string SureThing = "sure_thing";

    public const string LostCause = "lost_cause";

    public const string DoNotDisturb = "do_not_disturb";

    /// <summary>Every uplift segment, in the order the scoring rules decide them.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Persuadable, SureThing, LostCause, DoNotDisturb,
    ];
}
