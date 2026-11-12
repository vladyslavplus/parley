using Parley.Insights.Contracts.Events;
using Parley.Insights.Contracts.Scores;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Insights.Features.Priority.Rebuild;

/// <summary>Queues a priority rebuild when an uplift run completes, so the list ranks by the new scores.</summary>
public sealed class ScoringRunCompletedRebuildConsumer(PriorityRebuildScheduler scheduler, ITenantContext tenantContext)
    : IIntegrationEventConsumer<ScoringRunCompleted>
{
    public Task ConsumeAsync(ScoringRunCompleted integrationEvent, CancellationToken cancellationToken)
    {
        if (integrationEvent.ScoreKind == ScoreKinds.Uplift)
        {
            scheduler.Schedule(tenantContext.OrganizationId);
        }

        return Task.CompletedTask;
    }
}
