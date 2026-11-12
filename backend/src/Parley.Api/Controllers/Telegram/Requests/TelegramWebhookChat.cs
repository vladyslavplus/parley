using System.Text.Json.Serialization;

namespace Parley.Api.Controllers.Telegram.Requests;

/// <summary>The chat of a message. Only a private chat links an account.</summary>
public sealed record TelegramWebhookChat(
    [property: JsonPropertyName("id")] long? Id,
    [property: JsonPropertyName("type")] string? Type);
