namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>
/// Generates the demo workspace (contacts, orders, companies, deals, stage history, tasks, notes, tags and the timeline) into
/// one organization. Re-running with the same seed and reference instant changes nothing: rows have deterministic identifiers
/// or are keyed by source and external reference. The host checks the environment first.
/// </summary>
public interface ICrmDemoSeeder
{
    Task<CrmDemoImportReport> SeedAsync(CrmDemoImportRequest request, CancellationToken cancellationToken);

    /// <summary>Removes every CRM row of the organization, so a stale demo can be generated again from scratch.</summary>
    Task WipeAsync(Guid organizationId, CancellationToken cancellationToken);
}
