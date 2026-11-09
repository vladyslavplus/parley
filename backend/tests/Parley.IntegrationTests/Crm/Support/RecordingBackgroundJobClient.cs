using System.Collections.Concurrent;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>
/// Hangfire client that records every job it is asked to create, then passes the call on to the real client,
/// so tests can assert which jobs a sweep queued.
/// </summary>
public sealed class RecordingBackgroundJobClient(IBackgroundJobClient inner) : IBackgroundJobClient
{
    private readonly ConcurrentQueue<Job> _created = new();

    /// <summary>Every job created through this client, in creation order.</summary>
    public IReadOnlyCollection<Job> Created => _created.ToArray();

    public string Create(Job job, IState state)
    {
        _created.Enqueue(job);
        return inner.Create(job, state);
    }

    public bool ChangeState(string jobId, IState state, string? expectedState) =>
        inner.ChangeState(jobId, state, expectedState);
}
