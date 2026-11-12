namespace Parley.Notifications.Domain.Telegram.Commands;

/// <summary>
/// Reads a bot message. Commands start with a slash, may carry a bot suffix (<c>/today@ParleyBot</c>) and one argument.
/// Start arguments are kept as sent; the link code is checked later.
/// </summary>
public static class TelegramCommandParser
{
    private const char CommandPrefix = '/';

    private const char BotSuffixSeparator = '@';

    public static TelegramCommand Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim()[0] != CommandPrefix)
        {
            return new TelegramCommand(TelegramCommandKind.Unknown, null);
        }

        var parts = text.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var name = parts[0][1..];
        var suffix = name.IndexOf(BotSuffixSeparator, StringComparison.Ordinal);
        if (suffix >= 0)
        {
            name = name[..suffix];
        }

        var argument = parts.Length > 1 ? parts[1] : null;
        return name.ToLowerInvariant() switch
        {
            "start" => new TelegramCommand(TelegramCommandKind.Start, argument),
            "help" => new TelegramCommand(TelegramCommandKind.Help, null),
            "unlink" => new TelegramCommand(TelegramCommandKind.Unlink, null),
            "today" => new TelegramCommand(TelegramCommandKind.Today, argument),
            _ => new TelegramCommand(TelegramCommandKind.Unknown, null),
        };
    }
}
