using System.Globalization;

namespace Parley.Crm.Shared.Deals;

/// <summary>Text form of the non-text sort keys of the deal list, as cursors carry them.</summary>
public static class DealSortKeys
{
    private const NumberStyles AmountStyle = NumberStyles.Number;

    private const NumberStyles PositionStyle = NumberStyles.AllowLeadingSign;

    public static string FormatAmount(decimal value) => value.ToString(CultureInfo.InvariantCulture);

    public static decimal? ParseAmount(string key) =>
        decimal.TryParse(key, AmountStyle, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    public static string FormatPosition(long value) => value.ToString(CultureInfo.InvariantCulture);

    public static long? ParsePosition(string key) =>
        long.TryParse(key, PositionStyle, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
}
