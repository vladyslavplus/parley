namespace Parley.Crm.Shared.Tasks;

/// <summary>Filters of the task lists. <see cref="Now"/> is the instant overdue tasks are judged against.</summary>
public sealed record TaskListFilter(
    string? Search,
    Guid? AssigneeUserId,
    Guid? ContactId,
    Guid? DealId,
    Guid? CompanyId,
    string? Status,
    DateTime Now);
