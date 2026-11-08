namespace Parley.SharedKernel.Mediator.Transactions;

/// <summary>
/// A unit of transactional work. Persistence implementations plug in here.
/// </summary>
public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
