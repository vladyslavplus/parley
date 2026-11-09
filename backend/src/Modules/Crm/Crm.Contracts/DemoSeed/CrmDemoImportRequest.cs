namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>Input of one CRM demo import.</summary>
/// <param name="OrganizationId">Organization that receives the rows.</param>
/// <param name="InputDirectory">Directory with the demo export files.</param>
/// <param name="Currency">Currency of deals; equals the organization currency.</param>
/// <param name="Locale">Locale of the default pipeline names.</param>
/// <param name="SalesOwnerUserIds">Accounts that own the generated contacts, in a fixed order.</param>
public sealed record CrmDemoImportRequest(
    Guid OrganizationId,
    string InputDirectory,
    string Currency,
    string Locale,
    IReadOnlyList<Guid> SalesOwnerUserIds);
