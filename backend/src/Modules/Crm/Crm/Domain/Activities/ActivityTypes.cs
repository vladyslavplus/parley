namespace Parley.Crm.Domain.Activities;

/// <summary>Kinds of timeline entries. A new value is a code change.</summary>
public static class ActivityTypes
{
    public const string ContactCreated = "contact_created";

    public const string DealStageChanged = "deal_stage_changed";

    public const string DealClosed = "deal_closed";

    public const string NoteAdded = "note_added";

    public const string TaskCompleted = "task_completed";

    public const string OrderRecorded = "order_recorded";

    public static IReadOnlyList<string> All { get; } =
    [
        ContactCreated,
        DealStageChanged,
        DealClosed,
        NoteAdded,
        TaskCompleted,
        OrderRecorded,
    ];
}
