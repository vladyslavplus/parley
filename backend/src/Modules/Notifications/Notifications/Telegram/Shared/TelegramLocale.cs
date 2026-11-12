using Parley.Notifications.Sending;
using Parley.SharedKernel.Localization;

namespace Parley.Notifications.Telegram.Shared;

/// <summary>The culture of a Telegram text: the user's Telegram language when the app supports it, otherwise the default.</summary>
public static class TelegramLocale
{
    public static string Resolve(string? languageCode) => EmailCultureResolver.Resolve(languageCode, CultureCodes.Default);
}
