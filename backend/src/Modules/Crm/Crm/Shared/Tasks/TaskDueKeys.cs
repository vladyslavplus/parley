namespace Parley.Crm.Shared.Tasks;

/// <summary>Sort key of a task without a due date: later than any real due instant, so such tasks sort last.</summary>
public static class TaskDueKeys
{
    public static readonly DateTime NoDueDate = new(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);
}
