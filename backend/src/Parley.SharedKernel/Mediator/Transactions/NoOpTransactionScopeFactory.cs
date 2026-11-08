namespace Parley.SharedKernel.Mediator.Transactions;

/// <summary>
/// No-op factory used while no persistence exists.
/// </summary>
public sealed class NoOpTransactionScopeFactory : ITransactionScopeFactory
{
    public Task<ITransactionScope> BeginAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ITransactionScope>(new NoOpTransactionScope());
}
