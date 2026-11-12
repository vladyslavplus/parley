using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Parley.Notifications.Domain.Telegram;

namespace Parley.Notifications.Persistence.Configurations;

/// <summary>
/// Maps <see cref="TelegramLink"/>. Two partial unique indexes hold while a link is active: one link per user and organization,
/// and one user per chat and organization.
/// </summary>
public sealed class TelegramLinkConfiguration : IEntityTypeConfiguration<TelegramLink>
{
    public void Configure(EntityTypeBuilder<TelegramLink> builder)
    {
        builder.ToTable(NotificationsSchemaNames.TelegramLinksTable);

        builder.Property(link => link.Locale).HasMaxLength(TelegramLimits.LocaleMaximumLength);
        builder.Property(link => link.LinkedAt).HasColumnType("timestamptz");
        builder.Property(link => link.DeactivatedAt).HasColumnType("timestamptz");

        builder.HasIndex(link => new { link.OrganizationId, link.UserId })
            .IsUnique()
            .HasFilter("active")
            .HasDatabaseName(NotificationsSchemaNames.TelegramActiveUserLinkIndex);

        builder.HasIndex(link => new { link.OrganizationId, link.ChatId })
            .IsUnique()
            .HasFilter("active")
            .HasDatabaseName(NotificationsSchemaNames.TelegramActiveChatLinkIndex);
    }
}
