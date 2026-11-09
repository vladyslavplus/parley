using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Parley.Crm.Persistence.Configurations;

/// <summary>Converters that keep instants marked as UTC when they are read back from <c>timestamptz</c>.</summary>
public static class UtcValueConverters
{
    public static ValueConverter<DateTime, DateTime> Required { get; } = new(
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static ValueConverter<DateTime?, DateTime?> Optional { get; } = new(
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
}
