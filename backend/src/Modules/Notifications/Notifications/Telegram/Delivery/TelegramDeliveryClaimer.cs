using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Domain.Telegram;
using Parley.Notifications.Persistence;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>
/// Takes a batch of due deliveries for one worker. A claim sets a lease; rows with an expired lease can be taken again, so a
/// worker that dies loses no message. <c>FOR UPDATE SKIP LOCKED</c> lets several workers claim without waiting for each other.
/// The table is not tenant-filtered, so the sweep covers every organization.
/// </summary>
public sealed class TelegramDeliveryClaimer(NotificationsDbContext dbContext)
{
    private const string Table = NotificationsSchemaNames.Schema + "." + NotificationsSchemaNames.TelegramDeliveriesTable;

    private const string ClaimSql =
        "UPDATE " + Table + " AS delivery "
        + "SET claimed_until = {1} "
        + "WHERE delivery.id IN ("
        + "SELECT candidate.id FROM " + Table + " AS candidate "
        + "WHERE candidate.status = '" + TelegramDeliveryStatuses.Pending + "' "
        + "AND candidate.next_attempt_at <= {0} "
        + "AND (candidate.claimed_until IS NULL OR candidate.claimed_until <= {2}) "
        + "ORDER BY candidate.next_attempt_at "
        + "LIMIT {3} "
        + "FOR UPDATE SKIP LOCKED) "
        + "RETURNING delivery.id, delivery.organization_id, delivery.notification_id, delivery.user_id, "
        + "delivery.chat_id, delivery.attempts";

    public Task<List<ClaimedTelegramDelivery>> ClaimAsync(DateTime now, TimeSpan lease, int batchSize, CancellationToken cancellationToken) =>
        dbContext.Database
            .SqlQueryRaw<ClaimedTelegramDelivery>(ClaimSql, now, now.Add(lease), now, batchSize)
            .ToListAsync(cancellationToken);
}
