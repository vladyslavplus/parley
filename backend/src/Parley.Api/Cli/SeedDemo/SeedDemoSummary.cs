using Parley.Crm.Contracts.DemoSeed;

namespace Parley.Api.Cli.SeedDemo;

/// <summary>Printed result of <c>seed-demo</c>. Counts, file path and the CRM report only; no passwords.</summary>
/// <param name="OrganizationCreated">True when this run created the demo organization.</param>
/// <param name="TeamsCreated">Teams created by this run.</param>
/// <param name="UsersCreated">Accounts created by this run.</param>
/// <param name="MembershipsCreated">Memberships created by this run.</param>
/// <param name="AccountsFile">File that received the new passwords; null when this run created no account.</param>
/// <param name="Crm">Counts of the CRM import.</param>
public sealed record SeedDemoSummary(
    bool OrganizationCreated,
    int TeamsCreated,
    int UsersCreated,
    int MembershipsCreated,
    string? AccountsFile,
    CrmDemoImportReport Crm);
