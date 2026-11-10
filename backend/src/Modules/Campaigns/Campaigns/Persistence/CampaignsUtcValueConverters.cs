using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Parley.Campaigns.Persistence;

/// <summary>Converters that keep instants marked as UTC when they are read back from <c>timestamptz</c>.</summary>
public static class CampaignsUtcValueConverters
{
    public static ValueConverter<DateTime?, DateTime?> Optional { get; } = new(
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value,
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value);
}
