namespace Parley.Notifications.Telegram.Bot;

/// <summary>
/// One update of the bot, reduced to what the channel reads. <paramref name="ChatId"/> is null for an update that is not a
/// private chat message; such an update is recorded as handled and otherwise ignored.
/// </summary>
/// <param name="UpdateId">Telegram's update id, unique per bot.</param>
/// <param name="ChatId">The private chat the message came from; null otherwise.</param>
/// <param name="Text">Text of the message; null for a message without text.</param>
/// <param name="LanguageCode">Language the user set in Telegram, such as <c>uk</c>; null when unknown.</param>
public sealed record TelegramIncomingUpdate(long UpdateId, long? ChatId, string? Text, string? LanguageCode);
