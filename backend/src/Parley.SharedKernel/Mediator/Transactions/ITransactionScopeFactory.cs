namespace Parley.SharedKernel.Mediator.Transactions;

/// <summary>
/// Opens transactional scopes.
/// </summary>
public interface ITransactionScopeFactory
{
    Task<ITransactionScope> BeginAsync(CancellationToken cancellationToken = default);
}
