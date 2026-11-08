namespace Parley.SharedKernel.Mediator.Transactions;

/// <summary>
/// No-op scope used while no persistence exists.
/// </summary>
public sealed class NoOpTransactionScope : ITransactionScope
{
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
