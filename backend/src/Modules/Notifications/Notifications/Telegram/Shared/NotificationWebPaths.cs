using System.Collections.Frozen;
using Parley.Notifications.Contracts.Types;

namespace Parley.Notifications.Telegram.Shared;

/// <summary>
/// Web path of every notification link code and of the pages the Telegram texts point at. The frontend keeps the same
/// routes; the codes never carry a path, so a route change touches this map only.
/// </summary>
public static class NotificationWebPaths
{
    /// <summary>The signed-in home of the app, where the insights list lives.</summary>
    public const string AppHome = "app";

    public const string ContactsPath = "app/contacts";

    public const string TasksPath = "app/tasks";

    public const string DealsPath = "app/deals";

    public const string CampaignsPath = "app/campaigns";

    public const string TeamPath = "app/settings/members";

    private static readonly FrozenDictionary<string, string> ByLinkCode = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [NotificationLinkRoutes.Tasks] = TasksPath,
        [NotificationLinkRoutes.Deals] = DealsPath,
        [NotificationLinkRoutes.Campaigns] = CampaignsPath,
        [NotificationLinkRoutes.Insights] = AppHome,
        [NotificationLinkRoutes.Team] = TeamPath,
    }.ToFrozenDictionary(StringComparer.Ordinal);

    /// <summary>The web path of a link code. An unknown code falls back to the app home instead of a missing page.</summary>
    public static string ForLinkRoute(string linkRoute) =>
        ByLinkCode.TryGetValue(linkRoute, out var path) ? path : AppHome;

    /// <summary>Absolute web address of one record: <c>{base}/{culture}/app/contacts/{id}</c>.</summary>
    public static string ContactRecord(Guid contactId) => $"{ContactsPath}/{contactId:D}";

    /// <summary>Absolute address of a web path in one culture, without a trailing slash on the base.</summary>
    public static string BuildUrl(string publicWebBaseUrl, string culture, string path) =>
        $"{publicWebBaseUrl.TrimEnd('/')}/{culture}/{path}";
}
