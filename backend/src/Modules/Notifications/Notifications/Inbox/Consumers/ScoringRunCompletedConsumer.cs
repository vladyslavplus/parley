using Parley.Insights.Contracts.Events;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Recipients;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>Tells the user who started a scoring run that its scores are ready. The scoring runner publishes this event when a run completes.</summary>
public sealed class ScoringRunCompletedConsumer(InAppNotificationWriter writer) : IIntegrationEventConsumer<ScoringRunCompleted>
{
    public async Task ConsumeAsync(ScoringRunCompleted integrationEvent, CancellationToken cancellationToken)
    {
        if (NotificationRecipients.ForScoringRunCompleted(integrationEvent.StartedByUserId) is not { } recipient)
        {
            return;
        }

        await writer.AddAsync(
            recipient,
            NotificationTypes.ScoringRunCompleted,
            NotificationParameters.ScoringRun(integrationEvent.ScoringRunId, integrationEvent.ScoreKind, integrationEvent.ModelVersion),
            NotificationLinkRoutes.Insights,
            integrationEvent.EventId,
            cancellationToken);
    }
}
