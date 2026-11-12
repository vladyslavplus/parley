using System.Globalization;
using Parley.SharedKernel.Localization;

namespace Parley.Notifications.Telegram.Shared;

/// <summary>Reads Telegram texts in an explicit culture, for code that runs without an HTTP request.</summary>
public static class TelegramTexts
{
    public static string Get(string key, string culture) => SharedTexts.Get(key, culture);

    public static string Format(string key, string culture, params object[] arguments) =>
        string.Format(CultureInfo.GetCultureInfo(culture), SharedTexts.Get(key, culture), arguments);
}
