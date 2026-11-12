using Parley.Crm.Contracts.Events;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Recipients;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>Tells the owner of a deal, unless they took it themselves.</summary>
public sealed class DealAssignedConsumer(InAppNotificationWriter writer) : IIntegrationEventConsumer<DealAssigned>
{
    public async Task ConsumeAsync(DealAssigned integrationEvent, CancellationToken cancellationToken)
    {
        if (NotificationRecipients.ForDealAssigned(integrationEvent.OwnerUserId, integrationEvent.ActorUserId) is not { } recipient)
        {
            return;
        }

        await writer.AddAsync(
            recipient,
            NotificationTypes.DealAssigned,
            NotificationParameters.Id(NotificationParameters.DealId, integrationEvent.DealId),
            NotificationLinkRoutes.Deals,
            integrationEvent.EventId,
            cancellationToken);
    }
}
