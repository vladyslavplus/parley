namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>An account that may receive demo tasks next to the contact owners.</summary>
/// <param name="UserId">The account.</param>
/// <param name="VisibleOwnerUserIds">
/// Owners of the contacts the account can see under its data scope; null when it sees every contact. A task goes to the
/// account only for a contact owned by one of them, so the task never points at a record the assignee cannot open.
/// </param>
public sealed record CrmDemoTaskAssignee(Guid UserId, IReadOnlyList<Guid>? VisibleOwnerUserIds);
