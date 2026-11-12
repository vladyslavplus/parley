namespace Parley.Notifications.Contracts.Types;

/// <summary>Delivery channels of a notification preference.</summary>
public static class NotificationChannels
{
    public const string InApp = "in_app";

    public const string Telegram = "telegram";

    /// <summary>Every channel, in the order the preferences list shows them.</summary>
    public static readonly IReadOnlyList<string> All = [InApp, Telegram];
}
