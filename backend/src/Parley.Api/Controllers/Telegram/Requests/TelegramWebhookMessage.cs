using System.Text.Json.Serialization;

namespace Parley.Api.Controllers.Telegram.Requests;

/// <summary>The message of an update: its text, the chat it came from, and its sender.</summary>
public sealed record TelegramWebhookMessage(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("chat")] TelegramWebhookChat? Chat,
    [property: JsonPropertyName("from")] TelegramWebhookSender? From);
