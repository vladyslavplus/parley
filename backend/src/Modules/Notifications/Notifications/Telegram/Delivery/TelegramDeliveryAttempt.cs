using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Parley.Identity.Contracts.Authorization;
using Parley.Notifications.Contracts.Errors;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Options;
using Parley.Notifications.Persistence;
using Parley.Notifications.Telegram.Bot;
using Parley.Notifications.Telegram.Link;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>
/// One send of one claimed delivery. The attempt is counted and saved before the text is built and the message is sent, so a
/// row that crashes before its outcome is still counted: after <c>DeliveryMaxAttempts</c> such attempts the row fails instead
/// of retrying for ever. The message is sent outside any transaction, and only then is the outcome saved in a short write.
/// A blocked chat loses its links. The tenant is set for this unit only and cleared when it ends.
/// </summary>
/// <remarks>
/// Delivery stays at-least-once: a crash after Telegram accepted the message and before the outcome was saved resends it after
/// the lease. The unique key stops duplicate rows, not duplicate sends, and this class does not claim otherwise.
/// </remarks>
public sealed class TelegramDeliveryAttempt(
    NotificationsDbContext dbContext,
    IMutableTenantContext tenantContext,
    IPermissionChecker permissionChecker,
    ITelegramBotClient bot,
    TelegramSendPacer pacer,
    TelegramChatUnlinker chatUnlinker,
    IOptions<TelegramOptions> telegramOptions,
    IOptions<NotificationsOptions> notificationsOptions,
    TimeProvider timeProvider,
    ILogger<TelegramDeliveryAttempt> logger)
{
    public async Task RunAsync(ClaimedTelegramDelivery claimed, CancellationToken cancellationToken)
    {
        tenantContext.Set(claimed.OrganizationId);
        try
        {
            var delivery = await dbContext.TelegramDeliveries
                .FirstOrDefaultAsync(candidate => candidate.Id == claimed.Id, cancellationToken);
            if (delivery is null || delivery.Status != TelegramDeliveryStatuses.Pending)
            {
                return;
            }

            if (delivery.Attempts >= telegramOptions.Value.DeliveryMaxAttempts)
            {
                // Every allowed attempt was taken and none reached an outcome: a crash loop ends here, not never.
                delivery.Status = TelegramDeliveryStatuses.Failed;
                delivery.ErrorCode = TelegramErrorCodes.DeliveryUnavailable;
                delivery.ClaimedUntil = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            var link = await dbContext.TelegramLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.UserId == claimed.UserId
                    && candidate.ChatId == claimed.ChatId
                    && candidate.Active, cancellationToken);
            var notification = await dbContext.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(candidate => candidate.Id == claimed.NotificationId, cancellationToken);
            // A deactivated member keeps no delivery: the chat link may still be on, so the membership is checked as well.
            var activeMember = link is not null
                && await permissionChecker.IsActiveMemberAsync(claimed.UserId, claimed.OrganizationId, cancellationToken);
            if (link is null || notification is null || !activeMember)
            {
                delivery.Status = TelegramDeliveryStatuses.Cancelled;
                delivery.ClaimedUntil = null;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            // Counted before the text is built and the message is sent: a crash after this save uses the attempt up.
            delivery.Attempts += 1;
            await dbContext.SaveChangesAsync(cancellationToken);

            var text = TelegramNotificationText.Compose(
                notification.Type,
                link.Locale,
                notification.LinkRoute,
                notificationsOptions.Value.PublicWebBaseUrl);

            var (outcome, retryAfter) = await SendAsync(claimed.ChatId, text, cancellationToken);
            await RecordAsync(delivery, outcome, retryAfter, claimed.ChatId, cancellationToken);
        }
        finally
        {
            tenantContext.Clear();
        }
    }

    /// <summary>Sends one message. Returns the outcome and the retry time Telegram gave, if any. Nothing here touches the database.</summary>
    private async Task<(TelegramSendOutcome Outcome, TimeSpan? RetryAfter)> SendAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        await pacer.WaitTurnAsync(chatId, cancellationToken);
        try
        {
            await bot.SendTextAsync(chatId, text, cancellationToken);
            return (TelegramSendOutcome.Sent, null);
        }
        catch (TelegramCallException exception)
        {
            logger.LogWarning("Telegram delivery attempt failed. StatusCode={StatusCode}", exception.StatusCode);
            return (TelegramDeliveryRules.Classify(exception.StatusCode), exception.RetryAfter);
        }
    }

    private async Task RecordAsync(
        TelegramDelivery delivery,
        TelegramSendOutcome outcome,
        TimeSpan? retryAfter,
        long chatId,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var settings = telegramOptions.Value;
        delivery.ClaimedUntil = null;
        delivery.ErrorCode = TelegramDeliveryRules.ErrorCodeOf(outcome);

        if (outcome == TelegramSendOutcome.Sent)
        {
            delivery.Status = TelegramDeliveryStatuses.Sent;
            delivery.SentAt = now;
            delivery.ErrorCode = null;
        }
        else if (TelegramDeliveryRules.ShouldRetry(outcome, delivery.Attempts, settings.DeliveryMaxAttempts))
        {
            delivery.Status = TelegramDeliveryStatuses.Pending;
            delivery.NextAttemptAt = TelegramDeliveryRules.NextAttemptAt(now, delivery.Attempts, settings.DeliveryBaseBackoff, retryAfter);
        }
        else
        {
            delivery.Status = TelegramDeliveryRules.StatusOf(outcome);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (outcome == TelegramSendOutcome.Blocked)
        {
            // The chat blocked the bot: no link of the chat can deliver anything, so every link of the chat ends.
            await chatUnlinker.EndChatLinksAsync(chatId, cancellationToken);
        }
    }
}
