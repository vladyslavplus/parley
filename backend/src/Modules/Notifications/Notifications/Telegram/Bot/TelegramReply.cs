namespace Parley.Notifications.Telegram.Bot;

/// <summary>A text the bot answers with, to one chat. It is sent after the handling transaction has committed.</summary>
public sealed record TelegramReply(long ChatId, string Text);
