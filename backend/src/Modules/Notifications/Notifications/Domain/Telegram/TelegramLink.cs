using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Notifications.Domain.Telegram;

/// <summary>
/// A Telegram chat linked to one user in one organization. A user holds one active link per organization. A chat serves one
/// user per organization, and the same chat may serve several organizations. A link ends by unlinking, by a blocked chat, or by
/// the end of the membership; the row stays with <see cref="Active"/> false.
/// </summary>
public sealed class TelegramLink : BusinessEntity
{
    public Guid UserId { get; set; }

    public long ChatId { get; set; }

    /// <summary>Culture of the bot texts and the notification texts, taken from the chat's language when it was linked.</summary>
    public string Locale { get; set; } = string.Empty;

    public bool Active { get; set; }

    public DateTime LinkedAt { get; set; }

    public DateTime? DeactivatedAt { get; set; }
}
