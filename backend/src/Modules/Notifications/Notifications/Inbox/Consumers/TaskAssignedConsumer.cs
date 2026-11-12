using Parley.Crm.Contracts.Events;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Recipients;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>Tells the assignee of a task, unless they assigned it to themselves.</summary>
public sealed class TaskAssignedConsumer(InAppNotificationWriter writer) : IIntegrationEventConsumer<TaskAssigned>
{
    public async Task ConsumeAsync(TaskAssigned integrationEvent, CancellationToken cancellationToken)
    {
        if (NotificationRecipients.ForTaskAssigned(integrationEvent.AssigneeUserId, integrationEvent.ActorUserId) is not { } recipient)
        {
            return;
        }

        await writer.AddAsync(
            recipient,
            NotificationTypes.TaskAssigned,
            NotificationParameters.Id(NotificationParameters.TaskId, integrationEvent.TaskId),
            NotificationLinkRoutes.Tasks,
            integrationEvent.EventId,
            cancellationToken);
    }
}
