using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Notifications.Domain.Telegram;

namespace Parley.Notifications.Persistence.Configurations;

/// <summary>Maps <see cref="TelegramProcessedUpdate"/>. The Telegram update id is the key; the received time serves the cleanup.</summary>
public sealed class TelegramProcessedUpdateConfiguration : IEntityTypeConfiguration<TelegramProcessedUpdate>
{
    public void Configure(EntityTypeBuilder<TelegramProcessedUpdate> builder)
    {
        builder.ToTable(NotificationsSchemaNames.TelegramUpdatesTable);
        builder.HasKey(update => update.UpdateId);
        builder.Property(update => update.ReceivedAt).HasColumnType("timestamptz");

        builder.HasIndex(update => update.ReceivedAt)
            .HasDatabaseName(NotificationsSchemaNames.TelegramUpdateRetentionIndex);
    }
}
