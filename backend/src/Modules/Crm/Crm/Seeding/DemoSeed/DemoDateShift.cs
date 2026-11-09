namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// One constant shift for every order instant. The latest source order lands on the same time of day one day before
/// the seed instant, so the newest order is yesterday and no date is in the future. Intervals between orders stay real.
/// </summary>
public static class DemoDateShift
{
    public static TimeSpan OffsetFor(DateTime latestSourceOrder, DateTimeOffset seedInstant)
    {
        if (latestSourceOrder.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The source instant must be UTC.", nameof(latestSourceOrder));
        }

        var yesterday = DateOnly.FromDateTime(seedInstant.UtcDateTime).AddDays(-1);
        var target = yesterday.ToDateTime(TimeOnly.FromTimeSpan(latestSourceOrder.TimeOfDay), DateTimeKind.Utc);
        return target - latestSourceOrder;
    }
}
