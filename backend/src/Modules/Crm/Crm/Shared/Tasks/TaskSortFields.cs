namespace Parley.Crm.Shared.Tasks;

/// <summary>Sort fields accepted by the task lists. Ties are broken by identifier.</summary>
public static class TaskSortFields
{
    /// <summary>Due instant, tasks without a due date last. Default.</summary>
    public const string Due = "due";

    /// <summary>Title, case-insensitive.</summary>
    public const string Title = "title";

    /// <summary>Creation instant.</summary>
    public const string Created = "created";

    public static IReadOnlyList<string> Values { get; } = [Due, Title, Created];

    public static bool IsValid(string? value) =>
        value is null || Values.Contains(value);
}
