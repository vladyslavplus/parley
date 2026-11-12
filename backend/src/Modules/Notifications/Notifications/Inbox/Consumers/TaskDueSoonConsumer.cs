using Parley.Crm.Contracts.Events;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Recipients;
using Parley.SharedKernel.Outbox.Consumers;

namespace Parley.Notifications.Inbox.Consumers;

/// <summary>Reminds the assignee of an open task that is due soon.</summary>
public sealed class TaskDueSoonConsumer(InAppNotificationWriter writer) : IIntegrationEventConsumer<TaskDueSoon>
{
    public async Task ConsumeAsync(TaskDueSoon integrationEvent, CancellationToken cancellationToken)
    {
        if (NotificationRecipients.ForTaskDueSoon(integrationEvent.AssigneeUserId) is not { } recipient)
        {
            return;
        }

        await writer.AddAsync(
            recipient,
            NotificationTypes.TaskDueSoon,
            NotificationParameters.TaskDue(integrationEvent.TaskId, integrationEvent.DueAt),
            NotificationLinkRoutes.Tasks,
            integrationEvent.EventId,
            cancellationToken);
    }
}
