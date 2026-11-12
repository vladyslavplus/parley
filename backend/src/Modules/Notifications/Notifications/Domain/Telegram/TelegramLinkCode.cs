using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Notifications.Domain.Telegram;

/// <summary>
/// A one-time code that links a chat to a user. Only the SHA-256 hash is stored. A code is redeemable once, before
/// <see cref="ExpiresAt"/>; redeeming it sets <see cref="UsedAt"/>.
/// </summary>
public sealed class TelegramLinkCode : BusinessEntity
{
    public Guid UserId { get; set; }

    public byte[] CodeHash { get; set; } = [];

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }
}
