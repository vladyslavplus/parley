namespace Parley.Notifications.Features.Shared;

/// <summary>The addressee's preference list with its version. A write must send the version it read.</summary>
/// <param name="Version">Version of the set; changes whenever any switch of the user changes.</param>
/// <param name="Items">One switch for every notification type.</param>
public sealed record NotificationPreferencesView(long Version, IReadOnlyList<NotificationPreferenceView> Items);
