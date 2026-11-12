using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Notifications.Domain.Telegram;

namespace Parley.Notifications.Persistence.Configurations;

/// <summary>Maps <see cref="TelegramLinkCode"/>. The hash is unique, so one code redeems at most one link.</summary>
public sealed class TelegramLinkCodeConfiguration : IEntityTypeConfiguration<TelegramLinkCode>
{
    public void Configure(EntityTypeBuilder<TelegramLinkCode> builder)
    {
        builder.ToTable(NotificationsSchemaNames.TelegramLinkCodesTable);

        builder.Property(code => code.CodeHash).HasColumnType("bytea");
        builder.Property(code => code.ExpiresAt).HasColumnType("timestamptz");
        builder.Property(code => code.UsedAt).HasColumnType("timestamptz");

        builder.HasIndex(code => code.CodeHash)
            .IsUnique()
            .HasDatabaseName(NotificationsSchemaNames.TelegramLinkCodeHashIndex);
    }
}
