namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>Both files of one export.</summary>
/// <param name="Contacts">Customers, in file order.</param>
/// <param name="Orders">Invoices, in file order.</param>
public sealed record DemoExport(IReadOnlyList<DemoContactRecord> Contacts, IReadOnlyList<DemoOrderRecord> Orders);
