using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Shared.Pipelines;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Shares and text keys of the generated workspace (companies, deals, tasks, notes). Shares are percentages of the demo
/// contacts; each decision uses its own bucket, so the decisions are independent of each other. Texts are resource keys,
/// resolved in the organization locale, so the same choice gives the same text in each language. Stage positions are
/// read from the default pipeline definition, never typed in twice.
/// </summary>
public static class DemoWorkspaceRules
{
    /// <summary>Share of contacts that get a company.</summary>
    public const int CompanySharePercent = 30;

    /// <summary>Share of contacts that may get a deal. Only those with an order in the recent window get one.</summary>
    public const int DealSharePercent = 60;

    /// <summary>Share of contacts that get a task.</summary>
    public const int TaskSharePercent = 40;

    /// <summary>Share of contacts that get a note.</summary>
    public const int NoteSharePercent = 25;

    /// <summary>Length of the window before the seed instant in which a customer counts as recently active.</summary>
    public const int RecentActivityDays = 90;

    /// <summary>Task due dates spread over this many days, centred on the seed instant.</summary>
    public const int TaskDueSpreadDays = 25;

    public const int TaskDueCenterDays = 12;

    /// <summary>Position of the first stage of the default pipeline, where every deal starts.</summary>
    public const int FirstStageIndex = 0;

    /// <summary>Position of the won stage of the default pipeline.</summary>
    private static int WonStageIndex { get; } = StageIndexOf(PipelineStageKinds.Won);

    /// <summary>Position of the lost stage of the default pipeline.</summary>
    public static int LostStageIndex { get; } = StageIndexOf(PipelineStageKinds.Lost);

    /// <summary>
    /// Stage position for each bucket: the open stages in pipeline order, with the first open stage taking three of the ten
    /// buckets and the later ones fewer, then won and lost, so open stages are more common than won or lost ones.
    /// </summary>
    public static IReadOnlyList<int> StageByBucket { get; } = BuildStageByBucket();

    public static IReadOnlyList<string> DealSuffixKeys { get; } =
    [
        "crm.demo.deal_suffix.reorder",
        "crm.demo.deal_suffix.wholesale",
        "crm.demo.deal_suffix.seasonal",
        "crm.demo.deal_suffix.review",
        "crm.demo.deal_suffix.price",
    ];

    public static IReadOnlyList<string> LostReasonKeys { get; } =
    [
        "crm.demo.lost_reason.price",
        "crm.demo.lost_reason.competitor",
        "crm.demo.lost_reason.budget",
    ];

    public static IReadOnlyList<string> TaskTitleKeys { get; } =
    [
        "crm.demo.task.confirm_order",
        "crm.demo.task.send_price_list",
        "crm.demo.task.check_stock",
        "crm.demo.task.follow_up_delivery",
        "crm.demo.task.review_invoice",
    ];

    public static IReadOnlyList<string> NoteBodyKeys { get; } =
    [
        "crm.demo.note.delivery_times",
        "crm.demo.note.email",
        "crm.demo.note.bulk_pricing",
        "crm.demo.note.seasonal_reorders",
        "crm.demo.note.call_after_order",
    ];

    private static int StageIndexOf(string kind) =>
        DefaultPipelineStages.All.Select((stage, index) => (stage, index))
            .First(entry => entry.stage.Kind == kind)
            .index;

    private static IReadOnlyList<int> BuildStageByBucket()
    {
        var open = DefaultPipelineStages.All
            .Select((stage, index) => (stage, index))
            .Where(entry => entry.stage.Kind == PipelineStageKinds.Open)
            .Select(entry => entry.index)
            .ToArray();

        // Ten buckets: three to the first open stage, two to each of the next two, one to the last open stage, then won and lost.
        return
        [
            open[0], open[0], open[0],
            open[1], open[1],
            open[2], open[2],
            open[3],
            WonStageIndex,
            LostStageIndex,
        ];
    }
}
