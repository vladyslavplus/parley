using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Notifications.Contracts.Types;
using Parley.Notifications.Domain.Notifications;

namespace Parley.Notifications.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Notification"/>. The source event identifier is unique per organization, user and type, which makes a
/// replayed event a no-op at the row level as well as in the inbox. The type is checked against the known types.
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable(NotificationsSchemaNames.NotificationsTable, table =>
            table.HasCheckConstraint("ck_notifications_type", TypeCheck(nameof(Notification.Type))));
        builder.Property(notification => notification.Type).HasColumnType("text");
        builder.Property(notification => notification.Parameters)
            .HasConversion(new NotificationParametersConverter(), new NotificationParametersComparer())
            .HasColumnType("jsonb");
        builder.Property(notification => notification.LinkRoute).HasColumnType("text");
        builder.Property(notification => notification.ReadAt).HasColumnType("timestamptz");
        builder.HasIndex(notification => new { notification.OrganizationId, notification.UserId, notification.Type, notification.SourceEventId })
            .IsUnique()
            .HasDatabaseName(NotificationsSchemaNames.NotificationSourceIndex);
        builder.HasIndex(notification => new { notification.OrganizationId, notification.UserId, notification.ReadAt, notification.CreatedAt })
            .IsDescending(false, false, false, true)
            .HasDatabaseName(NotificationsSchemaNames.NotificationListIndex);
    }

    private static string TypeCheck(string column) =>
        $"{column} IN ({string.Join(", ", NotificationTypes.All.Select(type => $"'{type}'"))})";
}
