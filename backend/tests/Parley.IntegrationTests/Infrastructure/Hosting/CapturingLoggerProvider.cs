using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Parley.IntegrationTests.Infrastructure.Hosting;

/// <summary>
/// Keeps every formatted log message and exception the host writes, so a test can
/// assert that secrets never reach the logs.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _entries = new();

    public IReadOnlyCollection<string> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            entries.Enqueue(formatter(state, exception));
            if (exception is not null)
            {
                entries.Enqueue(exception.ToString());
            }
        }
    }
}
