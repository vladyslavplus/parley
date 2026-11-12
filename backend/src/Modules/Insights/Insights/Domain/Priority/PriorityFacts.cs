namespace Parley.Insights.Domain.Priority;

/// <summary>The two CRM facts that raise a contact's priority.</summary>
/// <param name="HasDueTask">An open task of the contact is due.</param>
/// <param name="HasStaleDeal">An open deal of the contact has not moved for the stale period.</param>
public sealed record PriorityFacts(bool HasDueTask, bool HasStaleDeal);
