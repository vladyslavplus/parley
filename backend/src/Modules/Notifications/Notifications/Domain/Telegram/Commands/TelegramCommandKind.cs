namespace Parley.Notifications.Domain.Telegram.Commands;

/// <summary>The commands the bot answers. Any other text is answered with the help text.</summary>
public enum TelegramCommandKind
{
    /// <summary>The start command; its argument is the link code when one was sent.</summary>
    Start = 1,

    Help,

    Unlink,

    /// <summary>The priority list for today; its argument is the number of the organization when the chat has several.</summary>
    Today,

    /// <summary>Text that is not a known command.</summary>
    Unknown,
}
