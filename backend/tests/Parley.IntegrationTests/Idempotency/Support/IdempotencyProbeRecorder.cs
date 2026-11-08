namespace Parley.IntegrationTests.Idempotency.Support;

/// <summary>
/// Counts executions of the probe handler for this factory's tests.
/// </summary>
public sealed class IdempotencyProbeRecorder
{
    private int _count;

    private int _failNext;

    public int Count => Volatile.Read(ref _count);

    public int Increment() => Interlocked.Increment(ref _count);

    /// <summary>Makes the next probe execution throw after it is counted.</summary>
    public void FailNextExecution() => Volatile.Write(ref _failNext, 1);

    public bool TakeFailure() => Interlocked.Exchange(ref _failNext, 0) == 1;
}
