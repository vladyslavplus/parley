using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Preferences;
using Parley.Notifications.Persistence;

namespace Parley.Notifications.Features.Shared;

/// <summary>
/// Reads a user's preference list and its version. The list holds one switch for every type on every channel. The version is
/// the highest transaction id that wrote one of the user's rows, so every change to the list moves it. Writers take the user's
/// lock first, so the version check and the writes run as one step.
/// </summary>
public static class NotificationPreferenceReader
{
    private const string LockNamespace = "notifications.preferences";

    private const string PreferencesTable = "\"" + NotificationsSchemaNames.Schema + "\".\"" + NotificationsSchemaNames.PreferencesTable + "\"";

    private const string VersionSql =
        "SELECT COALESCE(MAX(xmin::text::bigint), 0) AS \"Value\" FROM " + PreferencesTable + " WHERE organization_id = {0} AND user_id = {1}";

    private const string LockSql = "SELECT pg_advisory_xact_lock(hashtext({0}))";

    public static async Task<NotificationPreferencesView> ReadAsync(NotificationsDbContext dbContext, Guid userId, CancellationToken cancellationToken)
    {
        var stored = await dbContext.Preferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId)
            .ToDictionaryAsync(preference => (preference.Type, preference.Channel), preference => preference.Enabled, cancellationToken);

        var items = new List<NotificationPreferenceView>(NotificationTypes.All.Count * NotificationChannels.All.Count);
        foreach (var type in NotificationTypes.All)
        {
            foreach (var channel in NotificationChannels.All)
            {
                items.Add(new NotificationPreferenceView(type, channel, IsEnabled(stored, type, channel)));
            }
        }

        var version = await ReadVersionAsync(dbContext, userId, cancellationToken);
        return new NotificationPreferencesView(version, items);
    }

    /// <summary>The types the user switched off for in-app; their notifications stay out of the in-app list and count.</summary>
    public static Task<List<string>> ReadDisabledInAppTypesAsync(NotificationsDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Preferences
            .AsNoTracking()
            .Where(preference => preference.UserId == userId
                && preference.Channel == NotificationChannels.InApp
                && !preference.Enabled)
            .Select(preference => preference.Type)
            .ToListAsync(cancellationToken);

    public static Task<long> ReadVersionAsync(NotificationsDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Database
            .SqlQueryRaw<long>(VersionSql, dbContext.TenantContext.OrganizationId, userId)
            .FirstAsync(cancellationToken);

    /// <summary>Takes the user's preference lock until the enclosing transaction ends. Call inside a transaction.</summary>
    public static Task LockAsync(NotificationsDbContext dbContext, Guid userId, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlRawAsync(LockSql, [$"{LockNamespace}:{userId:D}"], cancellationToken);

    private static bool IsEnabled(IReadOnlyDictionary<(string Type, string Channel), bool> stored, string type, string channel)
    {
        bool? switchValue = stored.TryGetValue((type, channel), out var enabled) ? enabled : null;
        return channel == NotificationChannels.Telegram
            ? NotificationPreferenceRules.IsTelegramEnabled(switchValue)
            : NotificationPreferenceRules.IsEnabled(switchValue);
    }
}
