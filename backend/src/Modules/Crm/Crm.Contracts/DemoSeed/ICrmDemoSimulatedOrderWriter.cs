namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>
/// Writes the orders that the demo campaigns simulate (source <c>demo_simulated</c>). It exists for the demo seed only:
/// it refuses every host that is not Development. A simulated order can therefore never reach a real workspace.
/// Re-running with the same orders changes nothing: rows are keyed by source and external reference.
/// </summary>
public interface ICrmDemoSimulatedOrderWriter
{
    /// <summary>Writes the orders and returns how many rows this call created.</summary>
    /// <exception cref="InvalidOperationException">The host environment is not Development.</exception>
    Task<int> WriteAsync(Guid organizationId, IReadOnlyList<CrmSimulatedOrder> orders, CancellationToken cancellationToken);
}
