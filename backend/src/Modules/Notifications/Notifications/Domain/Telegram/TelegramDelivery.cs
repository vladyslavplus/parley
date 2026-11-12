namespace Parley.Notifications.Domain.Telegram;

/// <summary>
/// One message to one chat for one in-app notification. The unique pair of notification and chat makes a replayed event add
/// nothing. It is not tenant-filtered: the delivery worker sweeps all organizations and sets the tenant for each delivery.
/// </summary>
public sealed class TelegramDelivery
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public Guid OrganizationId { get; set; }

    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public long ChatId { get; set; }

    /// <summary>One of <see cref="TelegramDeliveryStatuses"/>.</summary>
    public string Status { get; set; } = TelegramDeliveryStatuses.Pending;

    public int Attempts { get; set; }

    public DateTime NextAttemptAt { get; set; }

    /// <summary>End of the lease a worker holds while it sends; another worker takes the row after it ends.</summary>
    public DateTime? ClaimedUntil { get; set; }

    public DateTime? SentAt { get; set; }

    /// <summary>Stable error code of the last failed attempt; null when none failed.</summary>
    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; }
}
