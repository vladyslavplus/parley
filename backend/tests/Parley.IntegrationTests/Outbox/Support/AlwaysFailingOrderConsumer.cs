using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.IntegrationTests.Outbox.Support;

/// <summary>
/// Consumer that always fails so the dispatcher must back off and stop.
/// </summary>
public sealed class AlwaysFailingOrderConsumer : IIntegrationEventConsumer<TestFlakyOrder>
{
    private const string FailureMessage = "boom";

    public int Attempts { get; private set; }

    public Task ConsumeAsync(TestFlakyOrder integrationEvent, CancellationToken cancellationToken)
    {
        Attempts++;
        throw new InvalidOperationException(FailureMessage);
    }
}
