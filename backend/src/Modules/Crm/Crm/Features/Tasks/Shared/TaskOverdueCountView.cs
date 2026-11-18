namespace Parley.Crm.Features.Tasks.Shared;

/// <summary>How many open tasks are past their due instant, under the scope and filters of the request.</summary>
/// <param name="Count">Number of overdue tasks.</param>
public sealed record TaskOverdueCountView(int Count);
