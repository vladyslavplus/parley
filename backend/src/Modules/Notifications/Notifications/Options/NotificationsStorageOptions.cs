namespace Parley.Notifications.Options;

/// <summary>Storage settings of the notifications module. Bind to the <c>NotificationsStorage</c> section.</summary>
public sealed class NotificationsStorageOptions
{
    public const string SectionName = "NotificationsStorage";

    /// <summary>
    /// Applies pending notifications migrations at host start, in the Development environment only. Any other environment
    /// migrates through an explicit deployment step.
    /// </summary>
    public bool ApplyMigrationsOnStartup { get; set; }
}
