using Mediator;
using Microsoft.Extensions.Logging;

namespace Parley.SharedKernel.Mediator.Behaviors;

/// <summary>
/// Logs handled message types and their durations.
/// </summary>
public sealed class LoggingBehavior<TMessage, TResponse>(
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("Mediator.Logging");

    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        _logger.LogInformation("Handling {MessageType}", typeof(TMessage).Name);
        var response = await next(message, cancellationToken);
        _logger.LogInformation(
            "Handled {MessageType} in {DurationMs} ms",
            typeof(TMessage).Name,
            timeProvider.GetElapsedTime(startedAt).TotalMilliseconds);
        return response;
    }
}
