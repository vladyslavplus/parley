namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>Input of one CRM demo seed.</summary>
/// <param name="OrganizationId">Organization that receives the rows.</param>
/// <param name="Currency">Currency of orders and deals; equals the organization currency.</param>
/// <param name="Locale">Locale of the default pipeline and the generated texts.</param>
/// <param name="SalesOwnerUserIds">Accounts that own the generated contacts, in a fixed order.</param>
/// <param name="RandomSeed">Seed of every generated choice; the same seed and reference instant give the same rows.</param>
/// <param name="ReferenceInstant">The instant the workspace is generated for; orders end the day before it.</param>
/// <param name="Profile">Volumes and shares of the generated workspace.</param>
/// <param name="TaskLeadership">
/// Accounts (administrators, managers, marketer) that receive a share of the tasks next to the contact owners, each with the
/// contacts it can see; none means every task goes to the contact owner.
/// </param>
public sealed record CrmDemoImportRequest(
    Guid OrganizationId,
    string Currency,
    string Locale,
    IReadOnlyList<Guid> SalesOwnerUserIds,
    long RandomSeed,
    DateTimeOffset ReferenceInstant,
    CrmDemoProfile Profile,
    IReadOnlyList<CrmDemoTaskAssignee>? TaskLeadership = null);
