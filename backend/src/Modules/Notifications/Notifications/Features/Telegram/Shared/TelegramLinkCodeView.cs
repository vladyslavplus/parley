namespace Parley.Notifications.Features.Telegram.Shared;

/// <summary>A new link code, shown once. The caller opens <see cref="DeepLink"/> or sends the code to the bot before <see cref="ExpiresAt"/>.</summary>
/// <param name="Code">The code. Only its hash is stored.</param>
/// <param name="DeepLink">The bot link that starts the chat with the code.</param>
/// <param name="ExpiresAt">Instant after which the code is refused.</param>
public sealed record TelegramLinkCodeView(string Code, string DeepLink, DateTime ExpiresAt);
