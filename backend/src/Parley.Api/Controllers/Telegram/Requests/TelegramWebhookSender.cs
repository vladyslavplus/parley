using System.Text.Json.Serialization;

namespace Parley.Api.Controllers.Telegram.Requests;

/// <summary>The sender of a message. The language is the user's Telegram language.</summary>
public sealed record TelegramWebhookSender(
    [property: JsonPropertyName("language_code")] string? LanguageCode);
