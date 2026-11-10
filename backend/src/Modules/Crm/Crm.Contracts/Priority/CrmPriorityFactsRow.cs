namespace Parley.Crm.Contracts.Priority;

/// <summary>The two facts of a contact that raise its priority.</summary>
/// <param name="ContactId">The contact.</param>
/// <param name="HasDueTask">An open task of the contact is due at the reading instant or earlier.</param>
/// <param name="HasStaleDeal">An open deal of the contact was not updated since the stale cut-off.</param>
public sealed record CrmPriorityFactsRow(Guid ContactId, bool HasDueTask, bool HasStaleDeal);
