using Parley.Insights.Contracts.Events;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Insights.Features.Priority.Rebuild;

/// <summary>Queues a priority rebuild when the ML switch or the active uplift model changes, so the list follows the setting.</summary>
public sealed class MlSettingsChangedRebuildConsumer(PriorityRebuildScheduler scheduler) : IIntegrationEventConsumer<MlSettingsChanged>
{
    public Task ConsumeAsync(MlSettingsChanged integrationEvent, CancellationToken cancellationToken)
    {
        scheduler.Schedule(integrationEvent.OrganizationId);
        return Task.CompletedTask;
    }
}
