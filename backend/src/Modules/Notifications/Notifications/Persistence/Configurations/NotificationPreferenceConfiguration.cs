using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Preferences;

namespace Parley.Notifications.Persistence.Configurations;

/// <summary>
/// Maps <see cref="NotificationPreference"/>. One row per user, type and channel; the channel and the type are checked
/// against the known values.
/// </summary>
public sealed class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable(NotificationsSchemaNames.PreferencesTable, table =>
        {
            table.HasCheckConstraint("ck_preferences_type", TypeCheck(nameof(NotificationPreference.Type)));
            table.HasCheckConstraint("ck_preferences_channel", ChannelCheck(nameof(NotificationPreference.Channel)));
        });
        builder.Property(preference => preference.Type).HasColumnType("text");
        builder.Property(preference => preference.Channel).HasColumnType("text");
        builder.HasIndex(preference => new { preference.OrganizationId, preference.UserId, preference.Type, preference.Channel })
            .IsUnique()
            .HasDatabaseName(NotificationsSchemaNames.PreferenceIndex);
    }

    private static string TypeCheck(string column) =>
        $"{column} IN ({string.Join(", ", NotificationTypes.All.Select(type => $"'{type}'"))})";

    private static string ChannelCheck(string column) =>
        $"{column} IN ('{NotificationChannels.InApp}', '{NotificationChannels.Telegram}')";
}
