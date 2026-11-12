namespace Parley.Notifications.Features.Telegram.Shared;

/// <summary>Whether the Telegram channel runs here. Carries no secret: the token and the webhook secret are never returned.</summary>
/// <param name="Enabled">True when a token is configured and the channel is switched on.</param>
/// <param name="Mode">How updates arrive: <c>polling</c> or <c>webhook</c>.</param>
/// <param name="BotUsername">The bot's username for the deep link; null when the channel is off.</param>
public sealed record TelegramStatusView(bool Enabled, string Mode, string? BotUsername);
