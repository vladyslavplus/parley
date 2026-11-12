using global::Telegram.Bot;
using global::Telegram.Bot.Exceptions;
using global::Telegram.Bot.Types;
using global::Telegram.Bot.Types.Enums;
using Microsoft.Extensions.Options;
using Parley.Notifications.Options;

namespace Parley.Notifications.Telegram.Bot;

/// <summary>
/// The Telegram.Bot library behind <see cref="ITelegramBotClient"/>. The library is built on first use, so an app without a token
/// never creates it. Its own automatic retry is off: the delivery rules decide every retry and honor Telegram's retry time.
/// </summary>
public sealed class TelegramApiClient(IOptions<TelegramOptions> options, IHttpClientFactory httpClientFactory) : ITelegramBotClient
{
    /// <summary>Name of the HTTP client that carries the Telegram calls.</summary>
    public const string HttpClientName = "Telegram";

    private readonly Lazy<global::Telegram.Bot.TelegramBotClient> _client = new(() => Create(options.Value, httpClientFactory));

    public async Task<IReadOnlyList<TelegramIncomingUpdate>> GetUpdatesAsync(long? offset, int timeoutSeconds, CancellationToken cancellationToken)
    {
        try
        {
            var updates = await _client.Value.GetUpdates(
                ToLibraryOffset(offset),
                timeout: timeoutSeconds,
                allowedUpdates: [UpdateType.Message],
                cancellationToken: cancellationToken);
            return updates.Select(Map).ToArray();
        }
        catch (RequestException exception)
        {
            throw Translate(exception);
        }
    }

    public async Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        try
        {
            await _client.Value.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }
        catch (RequestException exception)
        {
            throw Translate(exception);
        }
    }

    /// <summary>
    /// The library reads update offsets as 32-bit integers. A larger offset cannot be sent, so the checked cast fails the poll
    /// loudly instead of asking for the wrong updates. The webhook path keeps 64-bit ids.
    /// </summary>
    private static int? ToLibraryOffset(long? offset) => offset is { } value ? checked((int)value) : null;

    private static global::Telegram.Bot.TelegramBotClient Create(TelegramOptions settings, IHttpClientFactory httpClientFactory)
    {
        if (!TelegramOptionRules.IsConfigured(settings))
        {
            throw new InvalidOperationException("Telegram is not configured; the bot client cannot be created.");
        }

        var clientOptions = new TelegramBotClientOptions(settings.BotToken) { RetryThreshold = 0 };
        return new global::Telegram.Bot.TelegramBotClient(clientOptions, httpClientFactory.CreateClient(HttpClientName));
    }

    private static TelegramIncomingUpdate Map(Update update)
    {
        var message = update.Message;
        var privateChat = message is not null && message.Chat.Type == ChatType.Private;
        return new TelegramIncomingUpdate(
            update.Id,
            privateChat ? message!.Chat.Id : null,
            message?.Text,
            message?.From?.LanguageCode);
    }

    private static TelegramCallException Translate(RequestException exception) => exception switch
    {
        ApiRequestException api => new TelegramCallException(
            api.ErrorCode,
            api.Parameters?.RetryAfter is { } seconds ? TimeSpan.FromSeconds(seconds) : null),
        _ => new TelegramCallException(null, null),
    };
}
