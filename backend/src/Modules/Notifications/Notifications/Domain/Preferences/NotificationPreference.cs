using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Notifications.Domain.Preferences;

/// <summary>
/// One user's switch for one notification type on one channel. A type with no row is on for that channel.
/// </summary>
public sealed class NotificationPreference : BusinessEntity
{
    public Guid UserId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Channel { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}
