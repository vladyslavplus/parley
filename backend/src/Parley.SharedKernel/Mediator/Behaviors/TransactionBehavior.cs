using Mediator;
using Parley.SharedKernel.Mediator.Transactions;

namespace Parley.SharedKernel.Mediator.Behaviors;

/// <summary>
/// Opens a transactional scope around the handler.
/// Currently a no-op boundary while no persistence exists.
/// </summary>
public sealed class TransactionBehavior<TMessage, TResponse>(
    ITransactionScopeFactory transactionScopeFactory) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var scope = await transactionScopeFactory.BeginAsync(cancellationToken);
        try
        {
            var response = await next(message, cancellationToken);
            await scope.CommitAsync(cancellationToken);
            return response;
        }
        catch
        {
            await scope.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
