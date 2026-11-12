using Parley.Notifications.Telegram.Bot;

namespace Parley.Notifications.Features.Telegram.Updates;

/// <summary>What handling one update produced: the reply to send, or none.</summary>
public sealed record TelegramUpdateResult(TelegramReply? Reply);
