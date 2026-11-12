namespace Parley.Notifications.Domain.Telegram.Commands;

/// <summary>A parsed bot message: the command and its single optional argument.</summary>
public sealed record TelegramCommand(TelegramCommandKind Kind, string? Argument);
