using Parley.Notifications.Telegram.Shared;

namespace Parley.Notifications.Telegram.Delivery;

/// <summary>
/// The text of a notification message: the type's sentence in the chat's culture, then the link to the web page of the
/// notification. The message carries no personal data: the sentence names the kind of event, and the page shows the rest.
/// </summary>
public static class TelegramNotificationText
{
    public static string Compose(string type, string culture, string? linkRoute, string publicWebBaseUrl)
    {
        var sentence = TelegramTexts.Get(TelegramTextKeys.NotificationPrefix + type, culture);
        if (linkRoute is null)
        {
            return sentence;
        }

        var path = NotificationWebPaths.ForLinkRoute(linkRoute);
        return $"{sentence}\n{NotificationWebPaths.BuildUrl(publicWebBaseUrl, culture, path)}";
    }
}
