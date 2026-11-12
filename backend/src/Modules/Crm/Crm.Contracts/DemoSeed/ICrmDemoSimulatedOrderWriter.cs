namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>
/// Writes the orders that the demo campaigns simulate (source <c>demo_simulated</c>). It exists for the demo seed only:
/// it refuses to run outside the Demo environment, and outside a Development run against a database named
/// <c>parley_demo</c> or <c>parley_demo_&lt;suffix&gt;</c>. A simulated order can therefore never reach a real workspace.
/// Re-running with the same orders changes nothing: rows are keyed by source and external reference.
/// </summary>
public interface ICrmDemoSimulatedOrderWriter
{
    /// <summary>Writes the orders and returns how many rows this call created.</summary>
    /// <exception cref="InvalidOperationException">The environment or the database is not a demo target.</exception>
    Task<int> WriteAsync(Guid organizationId, IReadOnlyList<CrmSimulatedOrder> orders, CancellationToken cancellationToken);
}
