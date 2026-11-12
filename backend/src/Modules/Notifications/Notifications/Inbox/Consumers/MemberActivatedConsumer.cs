using Parley.Identity.Contracts.Events;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Recipients;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>Tells the inviter that the invited user is now a member.</summary>
public sealed class MemberActivatedConsumer(InAppNotificationWriter writer) : IIntegrationEventConsumer<MemberActivated>
{
    public async Task ConsumeAsync(MemberActivated integrationEvent, CancellationToken cancellationToken)
    {
        if (NotificationRecipients.ForMemberActivated(integrationEvent.UserId, integrationEvent.InvitedByUserId) is not { } recipient)
        {
            return;
        }

        await writer.AddAsync(
            recipient,
            NotificationTypes.MemberActivated,
            NotificationParameters.Id(NotificationParameters.UserId, integrationEvent.UserId),
            NotificationLinkRoutes.Team,
            integrationEvent.EventId,
            cancellationToken);
    }
}
