using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Notifications.Domain.Telegram;

namespace Parley.Notifications.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TelegramDelivery"/>. The unique pair of notification and chat stops a replayed event from sending twice.
/// The due index serves the worker's sweep.
/// </summary>
public sealed class TelegramDeliveryConfiguration : IEntityTypeConfiguration<TelegramDelivery>
{
    public void Configure(EntityTypeBuilder<TelegramDelivery> builder)
    {
        builder.ToTable(NotificationsSchemaNames.TelegramDeliveriesTable, table =>
        {
            table.HasCheckConstraint("ck_telegram_deliveries_status", StatusCheck(nameof(TelegramDelivery.Status)));
        });

        builder.Property(delivery => delivery.Status).HasColumnType("text");
        builder.Property(delivery => delivery.ErrorCode).HasMaxLength(TelegramLimits.ErrorCodeMaximumLength);
        builder.Property(delivery => delivery.NextAttemptAt).HasColumnType("timestamptz");
        builder.Property(delivery => delivery.ClaimedUntil).HasColumnType("timestamptz");
        builder.Property(delivery => delivery.SentAt).HasColumnType("timestamptz");
        builder.Property(delivery => delivery.CreatedAt).HasColumnType("timestamptz");

        builder.HasIndex(delivery => new { delivery.NotificationId, delivery.ChatId })
            .IsUnique()
            .HasDatabaseName(NotificationsSchemaNames.TelegramDeliveryIndex);

        builder.HasIndex(delivery => new { delivery.Status, delivery.NextAttemptAt })
            .HasDatabaseName(NotificationsSchemaNames.TelegramDueDeliveryIndex);
    }

    private static string StatusCheck(string column) =>
        $"{column} IN ({string.Join(", ", TelegramDeliveryStatuses.All.Select(status => $"'{status}'"))})";
}
