using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Contracts.DemoSeed;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Notifications;
using Parley.Notifications.Inbox;
using Parley.Notifications.Persistence;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Features.DemoSeed;

/// <summary>
/// Writes a few demo notifications (an assigned task and a finished campaign) for each demo account. The source event id is
/// derived from the recipient and the type, so the unique key makes a re-run a no-op. Preferences are left alone: the default
/// switches show the types in the app.
/// </summary>
public sealed class DemoNotificationSeeder(
    NotificationsDbContext dbContext,
    IMutableTenantContext tenantContext) : IDemoNotificationSeeder
{
    public async Task SeedAsync(
        Guid organizationId,
        IReadOnlyList<Guid> recipientUserIds,
        Guid actorUserId,
        DateTimeOffset reference,
        CancellationToken cancellationToken)
    {
        tenantContext.Set(organizationId);
        try
        {
            var taskId = Derive("demo-notification-task", organizationId.ToString("D"));
            var campaignId = Derive("demo-notification-campaign", organizationId.ToString("D"));
            var dueAt = reference.AddDays(1).UtcDateTime;
            foreach (var userId in recipientUserIds)
            {
                await AddIfMissingAsync(
                    organizationId,
                    userId,
                    NotificationTypes.TaskAssigned,
                    NotificationParameters.TaskDue(taskId, dueAt),
                    NotificationLinkRoutes.Tasks,
                    Derive("demo-notification-source", $"{userId:D}:task"),
                    cancellationToken);
                await AddIfMissingAsync(
                    organizationId,
                    userId,
                    NotificationTypes.CampaignCompleted,
                    NotificationParameters.Id(NotificationParameters.CampaignId, campaignId),
                    NotificationLinkRoutes.Campaigns,
                    Derive("demo-notification-source", $"{userId:D}:campaign"),
                    cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    public async Task WipeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        await dbContext.TelegramDeliveries.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.Notifications.IgnoreQueryFilters()
            .Where(row => row.OrganizationId == organizationId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private async Task AddIfMissingAsync(
        Guid organizationId,
        Guid userId,
        string type,
        IReadOnlyDictionary<string, string> parameters,
        string linkRoute,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Notifications.IgnoreQueryFilters()
            .AnyAsync(row => row.OrganizationId == organizationId && row.UserId == userId && row.Type == type && row.SourceEventId == sourceEventId, cancellationToken))
        {
            return;
        }

        dbContext.Notifications.Add(new Notification
        {
            OrganizationId = organizationId,
            UserId = userId,
            Type = type,
            Parameters = new Dictionary<string, string>(parameters),
            LinkRoute = linkRoute,
            SourceEventId = sourceEventId,
        });
    }

    private static Guid Derive(string kind, string key) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(kind + ":" + key)));
}
