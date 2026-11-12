namespace Parley.Notifications.Contracts.Types;

/// <summary>
/// Page a notification links to. The backend maps each code to the web path for Telegram and email texts, and the frontend
/// maps the same codes to its routes for the in-app list.
/// </summary>
public static class NotificationLinkRoutes
{
    public const string Tasks = "tasks";

    public const string Deals = "deals";

    public const string Campaigns = "campaigns";

    public const string Insights = "insights";

    public const string Team = "team";
}
