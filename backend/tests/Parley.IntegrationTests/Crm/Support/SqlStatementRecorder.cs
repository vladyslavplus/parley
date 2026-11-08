using System.Collections.Concurrent;
using System.Data.Common;
using System.Diagnostics;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>
/// Records the SQL text of every command Entity Framework executes while subscribed. Used to
/// prove that a list endpoint reads its rows in one statement (no N+1).
/// </summary>
public sealed class SqlStatementRecorder : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string EntityFrameworkListenerName = "Microsoft.EntityFrameworkCore";
    private const string CommandExecutingEventName = "Microsoft.EntityFrameworkCore.Database.Command.CommandExecuting";
    private const string CommandPropertyName = "Command";

    private readonly ConcurrentQueue<string> _statements = new();
    private readonly List<IDisposable> _subscriptions = [];
    private readonly IDisposable _listenerSubscription;

    public SqlStatementRecorder()
    {
        _listenerSubscription = DiagnosticListener.AllListeners.Subscribe(this);
    }

    public IReadOnlyCollection<string> Statements => _statements.ToArray();

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == EntityFrameworkListenerName)
        {
            _subscriptions.Add(listener.Subscribe(this));
        }
    }

    public void OnNext(KeyValuePair<string, object?> entry)
    {
        if (entry.Key != CommandExecutingEventName || entry.Value is null)
        {
            return;
        }

        var command = entry.Value.GetType().GetProperty(CommandPropertyName)?.GetValue(entry.Value) as DbCommand;
        if (command is not null)
        {
            _statements.Enqueue(command.CommandText);
        }
    }

    public void OnError(Exception error)
    {
    }

    public void OnCompleted()
    {
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _listenerSubscription.Dispose();
    }
}
