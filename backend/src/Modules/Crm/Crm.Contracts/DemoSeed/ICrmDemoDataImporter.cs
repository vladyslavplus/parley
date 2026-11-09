namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>
/// Loads the demo export (contacts, orders) and the workspace records generated for those contacts (companies, deals,
/// stage history, tasks, notes, timeline) into one organization. Re-running with the same input changes nothing: rows
/// have deterministic identifiers or are keyed by source and external reference. The host checks the environment first.
/// </summary>
public interface ICrmDemoDataImporter
{
    Task<CrmDemoImportReport> ImportAsync(CrmDemoImportRequest request, CancellationToken cancellationToken);
}
