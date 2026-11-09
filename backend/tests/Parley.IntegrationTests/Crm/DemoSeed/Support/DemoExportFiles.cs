using System.IO.Compression;
using System.Text;

namespace Parley.IntegrationTests.Crm.DemoSeed.Support;

/// <summary>Writes a tiny demo export in the format the ML module produces (gzip CSV, fixed header).</summary>
public static class DemoExportFiles
{
    private const string ContactsFile = "contacts.csv.gz";

    private const string OrdersFile = "orders.csv.gz";

    /// <summary>Three customers (one only cancels), five invoices; the latest sale is 2011-12-09 12:50 UTC.</summary>
    public static string WriteSample(string directory)
    {
        Directory.CreateDirectory(directory);
        Write(Path.Combine(directory, ContactsFile), """
            customer_id,country_iso2
            101,GB
            102,DE
            103,
            """);
        Write(Path.Combine(directory, OrdersFile), """
            invoice_no,customer_id,ordered_at,total_amount,currency,line_count,is_cancellation
            1001,101,2011-06-01T10:00:00Z,50.00,GBP,3,0
            1002,101,2011-12-09T12:50:00Z,20.00,GBP,1,0
            C1003,101,2011-12-08T09:00:00Z,-20.00,GBP,1,1
            1004,102,2011-03-01T08:30:00Z,10.00,GBP,2,0
            C1005,103,2011-05-01T08:30:00Z,-5.00,GBP,1,1
            """);
        return directory;
    }

    /// <summary>
    /// Writes one order per customer for <paramref name="customerCount"/> customers, all in the last days of the export, so
    /// every customer counts as recently active and the owners are spread over the sales accounts.
    /// </summary>
    public static string WriteWorkspace(string directory, int customerCount)
    {
        Directory.CreateDirectory(directory);
        var contacts = new StringBuilder("customer_id,country_iso2\n");
        var orders = new StringBuilder("invoice_no,customer_id,ordered_at,total_amount,currency,line_count,is_cancellation\n");
        for (var index = 0; index < customerCount; index++)
        {
            var customerId = 2000 + index;
            contacts.Append(customerId).Append(",GB\n");
            var orderedAt = new DateTime(2011, 12, 1, 8, 0, 0, DateTimeKind.Utc).AddHours(index);
            orders.Append(5000 + index).Append(',').Append(customerId).Append(',')
                .Append(orderedAt.ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture))
                .Append(",25.00,GBP,2,0\n");
        }

        Write(Path.Combine(directory, ContactsFile), contacts.ToString().TrimEnd('\n'));
        Write(Path.Combine(directory, OrdersFile), orders.ToString().TrimEnd('\n'));
        return directory;
    }

    private static void Write(string path, string text)
    {
        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        gzip.Write(Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n") + "\n"));
    }
}
