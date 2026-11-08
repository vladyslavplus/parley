using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Parley.SharedKernel.Idempotency.Persistence;

/// <summary>
/// EF mapping for <c>Idempotency-Key</c> rows. Apply once per module
/// <c>DbContext</c> in <c>OnModelCreating</c> (after <c>HasDefaultSchema</c>
/// so the rows land in the module schema):
/// <c>modelBuilder.AddIdempotencyConventions();</c>
/// </summary>
public static class IdempotencyModelBuilderExtensions
{
    public static ModelBuilder AddIdempotencyConventions(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entry = modelBuilder.Entity<IdempotencyKey>();
        entry.ToTable(IdempotencyTableNames.IdempotencyKeys);
        entry.Property(entity => entity.Key).IsRequired();
        entry.Property(entity => entity.RequestHash).HasColumnType("bytea").IsRequired();
        entry.Property(entity => entity.ResponseStatus).IsRequired();
        entry.Property(entity => entity.ResponseBody).HasColumnType("bytea");
        entry.Property(entity => entity.ResponseContentType).HasColumnType("text");
        entry.Property(entity => entity.ResponseHeaders).HasColumnType("text");
        entry.Property(entity => entity.ExpiresAt).HasColumnType("timestamptz").HasConversion(UtcConverter);
        entry.HasIndex(entity => new { entity.OrganizationId, entity.Key })
            .HasDatabaseName(IdempotencyTableNames.UniqueKeyIndexName)
            .IsUnique();
        entry.HasIndex(entity => entity.ExpiresAt)
            .HasDatabaseName(IdempotencyTableNames.ExpiresIndexName);

        return modelBuilder;
    }

    private static readonly ValueConverter<DateTime, DateTime> UtcConverter = new(
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
