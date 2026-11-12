namespace Parley.Notifications.Domain.Telegram;

/// <summary>Column bounds of the Telegram tables.</summary>
public static class TelegramLimits
{
    /// <summary>Longest culture name stored with a link, such as <c>uk</c>.</summary>
    public const int LocaleMaximumLength = 8;

    /// <summary>Longest stable error code stored with a delivery.</summary>
    public const int ErrorCodeMaximumLength = 64;

    /// <summary>Largest webhook body read: an update is a short JSON object, so anything bigger is refused before it is parsed.</summary>
    public const int WebhookMaximumBodyBytes = 1024 * 1024;
}
