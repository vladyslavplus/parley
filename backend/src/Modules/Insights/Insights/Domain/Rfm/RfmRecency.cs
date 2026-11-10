namespace Parley.Insights.Domain.Rfm;

/// <summary>Recency of a contact: whole days between the last order and the reading instant.</summary>
public static class RfmRecency
{
    /// <summary>Whole days from <paramref name="lastOrderAt"/> to <paramref name="asOf"/>; never negative.</summary>
    public static int DaysBetween(DateTimeOffset asOf, DateTimeOffset lastOrderAt) =>
        (int)Math.Max(0, Math.Floor((asOf - lastOrderAt).TotalDays));
}
