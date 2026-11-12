namespace Parley.Notifications.Features.Telegram.Shared;

/// <summary>Whether the signed-in user has a Telegram chat linked in the current organization.</summary>
/// <param name="Linked">True when an active link exists.</param>
/// <param name="LinkedAt">When the link was made; null when none exists.</param>
public sealed record TelegramLinkView(bool Linked, DateTime? LinkedAt);
