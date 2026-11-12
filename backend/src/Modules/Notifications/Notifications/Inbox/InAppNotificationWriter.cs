using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Notifications;
using Parley.Notifications.Domain.Preferences;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Delivery;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Inbox;

/// <summary>
/// Adds one notification inside the consumer's transaction. A notification is written when the recipient wants it in the app
/// or on Telegram; the in-app list hides the types switched off for in-app. For a Telegram switch that is on, the planner stages
/// one delivery per linked chat. It never saves: the dispatcher commits the rows together with the inbox entry of the event.
/// </summary>
public sealed class InAppNotificationWriter(
    NotificationsDbContext dbContext,
    ITenantContext tenantContext,
    TelegramDeliveryPlanner telegramPlanner)
{
    public async Task AddAsync(
        Guid recipientUserId,
        string type,
        IReadOnlyDictionary<string, string> parameters,
        string linkRoute,
        Guid sourceEventId,
        CancellationToken cancellationToken)
    {
        var switches = await dbContext.Preferences
            .AsNoTracking()
            .Where(preference => preference.UserId == recipientUserId && preference.Type == type)
            .ToDictionaryAsync(preference => preference.Channel, preference => preference.Enabled, cancellationToken);

        bool? inAppStored = switches.TryGetValue(NotificationChannels.InApp, out var inApp) ? inApp : null;
        bool? telegramStored = switches.TryGetValue(NotificationChannels.Telegram, out var telegram) ? telegram : null;
        var wantsInApp = NotificationPreferenceRules.IsEnabled(inAppStored);
        var wantsTelegram = NotificationPreferenceRules.IsTelegramEnabled(telegramStored);
        if (!wantsInApp && !wantsTelegram)
        {
            return;
        }

        var notification = new Notification
        {
            OrganizationId = tenantContext.OrganizationId,
            UserId = recipientUserId,
            Type = type,
            Parameters = new Dictionary<string, string>(parameters),
            LinkRoute = linkRoute,
            SourceEventId = sourceEventId,
        };
        dbContext.Notifications.Add(notification);

        if (wantsTelegram)
        {
            await telegramPlanner.PlanAsync(notification, cancellationToken);
        }
    }
}
