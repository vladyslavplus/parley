namespace Parley.Crm.Domain.Tasks;

/// <summary>Kinds of task, stored as these codes. A new value is a code change and a new check constraint.</summary>
public static class TaskTypes
{
    public const string Call = "call";

    public const string Email = "email";

    public const string Meeting = "meeting";

    /// <summary>A generic to-do; the type of a task that names none.</summary>
    public const string Todo = "todo";

    /// <summary>The type of a task created without one.</summary>
    public const string Default = Todo;

    public static IReadOnlyList<string> All { get; } = [Call, Email, Meeting, Todo];

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
