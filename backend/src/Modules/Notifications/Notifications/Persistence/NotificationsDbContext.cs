using Microsoft.EntityFrameworkCore;
using Parley.Notifications.Domain.Notifications;
using Parley.Notifications.Domain.Preferences;
using Parley.Notifications.Domain.Telegram;
using Parley.SharedKernel.Outbox.Mapping;
using Parley.SharedKernel.Persistence.Conventions;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Notifications.Persistence;

/// <summary>
/// Notifications module database context on the <c>notifications</c> schema: the in-app notifications and the user
/// preferences. Outbox and inbox rows live here too, because the module consumes events and publishes its own later.
/// </summary>
public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, ITenantContext tenantContext)
    : DbContext(options), ITenantDbContext
{
    public const string SchemaName = NotificationsSchemaNames.Schema;

    public ITenantContext TenantContext { get; } = tenantContext;

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<NotificationPreference> Preferences => Set<NotificationPreference>();

    public DbSet<TelegramLink> TelegramLinks => Set<TelegramLink>();

    public DbSet<TelegramLinkCode> TelegramLinkCodes => Set<TelegramLinkCode>();

    public DbSet<TelegramDelivery> TelegramDeliveries => Set<TelegramDelivery>();

    public DbSet<TelegramProcessedUpdate> TelegramProcessedUpdates => Set<TelegramProcessedUpdate>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(SchemaName);
        base.OnModelCreating(builder);
        builder.AddTenantConventions(this);
        builder.AddOutboxConventions();
        builder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
