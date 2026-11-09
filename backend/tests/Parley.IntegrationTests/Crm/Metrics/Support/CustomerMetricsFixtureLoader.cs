using System.Globalization;
using System.Text.Json;

namespace Parley.IntegrationTests.Crm.Metrics.Support;

/// <summary>Reads the committed fixture written by the ML module's fixture generator.</summary>
public static class CustomerMetricsFixtureLoader
{
    private const string FixtureFolder = "Crm/Metrics/Fixtures";

    private const string SettingsFile = "fixture.json";

    private const string ContactsFile = "contacts.csv";

    private const string OrdersFile = "orders.csv";

    private const string OpenDealsFile = "open_deals.csv";

    private const string ExpectedFile = "expected_metrics.csv";

    private const string TrueText = "1";

    private const string Separator = ",";

    private const string AsOfProperty = "as_of";

    private const int ContactIdColumn = 0;

    private static readonly string[] ExpectedColumns =
    [
        "contact_id", "first_order_at", "last_order_at", "order_count", "purchase_day_count", "orders_short", "orders_long",
        "total_value", "value_long", "avg_order_value", "avg_repeat_order_value", "has_open_deal",
    ];

    public static CustomerMetricsFixtureData Load()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, FixtureFolder);
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, SettingsFile)));
        var asOf = DateTimeOffset.Parse(settings.RootElement.GetProperty(AsOfProperty).GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        var contacts = ReadRows(Path.Combine(folder, ContactsFile))
            .Select(columns => Guid.Parse(columns[ContactIdColumn]))
            .ToArray();
        var orders = ReadRows(Path.Combine(folder, OrdersFile))
            .Select(columns => new FixtureOrder(
                columns[0],
                Guid.Parse(columns[1]),
                DateTime.SpecifyKind(DateTime.Parse(columns[2], CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), DateTimeKind.Utc),
                decimal.Parse(columns[3], CultureInfo.InvariantCulture),
                columns[4] == TrueText))
            .ToArray();
        var openDeals = ReadRows(Path.Combine(folder, OpenDealsFile))
            .Select(columns => Guid.Parse(columns[ContactIdColumn]))
            .ToArray();
        var expected = ReadRows(Path.Combine(folder, ExpectedFile))
            .Select(ParseExpected)
            .ToArray();

        return new CustomerMetricsFixtureData(asOf, contacts, orders, openDeals, expected);
    }

    private static ExpectedCustomerMetrics ParseExpected(string[] columns)
    {
        var values = ExpectedColumns.Select((name, index) => (name, value: columns[index])).ToDictionary(pair => pair.name, pair => pair.value);
        return new ExpectedCustomerMetrics(
            Guid.Parse(values["contact_id"]),
            ParseInstant(values["first_order_at"]),
            ParseInstant(values["last_order_at"]),
            int.Parse(values["order_count"], CultureInfo.InvariantCulture),
            int.Parse(values["purchase_day_count"], CultureInfo.InvariantCulture),
            int.Parse(values["orders_short"], CultureInfo.InvariantCulture),
            int.Parse(values["orders_long"], CultureInfo.InvariantCulture),
            decimal.Parse(values["total_value"], CultureInfo.InvariantCulture),
            decimal.Parse(values["value_long"], CultureInfo.InvariantCulture),
            decimal.Parse(values["avg_order_value"], CultureInfo.InvariantCulture),
            decimal.Parse(values["avg_repeat_order_value"], CultureInfo.InvariantCulture),
            values["has_open_deal"] == TrueText);
    }

    private static DateTimeOffset? ParseInstant(string text) =>
        text.Length == 0 ? null : DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

    /// <summary>Rows of a CSV file without its header. The fixture has no quoted fields.</summary>
    private static IEnumerable<string[]> ReadRows(string path) =>
        File.ReadLines(path).Skip(1).Where(line => line.Length > 0).Select(line => line.Split(Separator));
}
