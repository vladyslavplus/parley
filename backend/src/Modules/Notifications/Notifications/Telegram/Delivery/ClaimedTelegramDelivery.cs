namespace Parley.Notifications.Telegram.Delivery;

/// <summary>One delivery a worker claimed for a send: the identifiers it needs, read from the claim.</summary>
public sealed class ClaimedTelegramDelivery
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }

    public long ChatId { get; set; }

    public int Attempts { get; set; }
}
