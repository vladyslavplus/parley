namespace Parley.Notifications.Domain.Preferences;

/// <summary>Decides whether a notification type reaches a user. A type with no stored preference is on: the default is to notify.</summary>
public static class NotificationPreferenceRules
{
    /// <param name="storedEnabled">The stored switch, or null when the user has never set it.</param>
    public static bool IsEnabled(bool? storedEnabled) => storedEnabled ?? true;

    /// <summary>
    /// The Telegram switch is off until the user turns it on for a type. A link alone does not send anything: a message needs
    /// both the link and the switch.
    /// </summary>
    public static bool IsTelegramEnabled(bool? storedEnabled) => storedEnabled ?? false;
}
