using Parley.Campaigns.Contracts.Events;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Recipients;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>Tells the user who started a campaign that every recipient now has a final outcome.</summary>
public sealed class CampaignCompletedConsumer(InAppNotificationWriter writer) : IIntegrationEventConsumer<CampaignCompleted>
{
    public async Task ConsumeAsync(CampaignCompleted integrationEvent, CancellationToken cancellationToken)
    {
        if (NotificationRecipients.ForCampaignCompleted(integrationEvent.StartedByUserId) is not { } recipient)
        {
            return;
        }

        await writer.AddAsync(
            recipient,
            NotificationTypes.CampaignCompleted,
            NotificationParameters.Id(NotificationParameters.CampaignId, integrationEvent.CampaignId),
            NotificationLinkRoutes.Campaigns,
            integrationEvent.EventId,
            cancellationToken);
    }
}
