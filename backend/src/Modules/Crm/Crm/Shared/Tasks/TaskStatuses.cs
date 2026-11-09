namespace Parley.Crm.Shared.Tasks;

/// <summary>Status filter of the task lists.</summary>
public static class TaskStatuses
{
    /// <summary>Not completed, due or not yet due.</summary>
    public const string Open = "open";

    /// <summary>Completed.</summary>
    public const string Completed = "completed";

    /// <summary>Not completed and past its due instant.</summary>
    public const string Overdue = "overdue";

    public static IReadOnlyList<string> All { get; } = [Open, Completed, Overdue];
}
