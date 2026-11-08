using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Parley.SharedKernel.Outbox.Messages;

namespace Parley.SharedKernel.Outbox.Mapping;

/// <summary>
/// EF mappings for the transactional outbox. Apply once per module
/// <c>DbContext</c> in <c>OnModelCreating</c> (after
/// <c>HasDefaultSchema</c> so the rows land in the module schema):
/// <c>modelBuilder.AddOutboxConventions();</c>
/// </summary>
public static class OutboxModelBuilderExtensions
{
    private const string DueRowsFilter = "\"processed_at\" IS NULL AND \"dead_lettered_at\" IS NULL";

    public static ModelBuilder AddOutboxConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var outbox = modelBuilder.Entity<OutboxMessage>();
        outbox.ToTable(OutboxTableNames.OutboxMessages);
        outbox.HasKey(entity => entity.Id);
        outbox.Property(entity => entity.Type).IsRequired();
        outbox.Property(entity => entity.Payload).HasColumnType("jsonb").IsRequired();
        outbox.Property(entity => entity.OccurredAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
        outbox.Property(entity => entity.ProcessedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
        outbox.Property(entity => entity.DeadLetteredAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
        outbox.Property(entity => entity.NextAttemptAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
        outbox.HasIndex(entity => entity.NextAttemptAt).HasFilter(DueRowsFilter);

        var inbox = modelBuilder.Entity<InboxMessage>();
        inbox.ToTable(OutboxTableNames.InboxMessages);
        inbox.HasKey(entity => new { entity.Consumer, entity.EventId });
        inbox.Property(entity => entity.Consumer).IsRequired();
        inbox.Property(entity => entity.ReceivedAt).HasColumnType("timestamptz").HasConversion(UtcConverter);

        return modelBuilder;
    }

    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
