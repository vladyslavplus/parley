namespace Parley.Notifications.Options;

/// <summary>Hard bounds the notification options must stay within, whatever the configuration says.</summary>
public static class NotificationsOptionLimits
{
    public const int MinimumPageSize = 1;

    public const int MaximumAllowedPageSize = 200;
}
