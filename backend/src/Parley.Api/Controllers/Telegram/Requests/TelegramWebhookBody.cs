using System.Text.Json.Serialization;
using Parley.Notifications.Telegram.Bot;

namespace Parley.Api.Controllers.Telegram.Requests;

/// <summary>The part of a Telegram update the channel reads: its id and the message, when there is one.</summary>
public sealed record TelegramWebhookBody(
    [property: JsonPropertyName("update_id")] long? UpdateId,
    [property: JsonPropertyName("message")] TelegramWebhookMessage? Message)
{
    /// <summary>The update for the handler, or null when the id is missing. A message outside a private chat has no chat id.</summary>
    public TelegramIncomingUpdate? ToIncoming()
    {
        if (UpdateId is not { } updateId)
        {
            return null;
        }

        var privateChat = Message?.Chat?.Type == "private";
        return new TelegramIncomingUpdate(
            updateId,
            privateChat ? Message?.Chat?.Id : null,
            Message?.Text,
            Message?.From?.LanguageCode);
    }
}

