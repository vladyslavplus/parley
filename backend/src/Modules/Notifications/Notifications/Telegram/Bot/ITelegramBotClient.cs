namespace Parley.Notifications.Telegram.Bot;

/// <summary>
/// The bot calls the channel needs: read updates and send text. The Telegram library sits behind this interface, so tests use a
/// fake and never reach the network. Failures surface as <see cref="TelegramCallException"/>.
/// </summary>
public interface ITelegramBotClient
{
    /// <summary>Updates after <paramref name="offset"/>, waiting up to <paramref name="timeoutSeconds"/> for new ones.</summary>
    Task<IReadOnlyList<TelegramIncomingUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken);

    /// <summary>Sends one plain text message to a chat.</summary>
    Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken);
}
