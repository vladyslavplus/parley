using System.Globalization;
using System.IO.Compression;
using System.Text;
using Parley.Crm.Contracts.DemoSeed;

namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Reads the gzip CSV files of the demo export. Header and column count are checked; a malformed line is reported
/// with its file and line number, never with its content, so no personal data reaches the logs.
/// </summary>
public static class DemoExportReader
{
    public static DemoExport Read(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        var contactsPath = Path.Combine(directory, DemoSeedConstants.ContactsFileName);
        var ordersPath = Path.Combine(directory, DemoSeedConstants.OrdersFileName);
        if (!File.Exists(contactsPath) || !File.Exists(ordersPath))
        {
            throw new FileNotFoundException("The demo export must contain the contacts and orders files.", directory);
        }

        return new DemoExport(ReadContacts(contactsPath), ReadOrders(ordersPath));
    }

    private static IReadOnlyList<DemoContactRecord> ReadContacts(string path)
    {
        var contacts = new List<DemoContactRecord>();
        foreach (var (fields, line) in ReadRows(path, DemoSeedConstants.ContactColumns))
        {
            var customerRef = RequiredField(fields, 0, path, line);
            var country = fields[1].Length == 0 ? null : fields[1];
            contacts.Add(new DemoContactRecord(customerRef, country));
        }

        return contacts;
    }

    private static IReadOnlyList<DemoOrderRecord> ReadOrders(string path)
    {
        var orders = new List<DemoOrderRecord>();
        foreach (var (fields, line) in ReadRows(path, DemoSeedConstants.OrderColumns))
        {
            var currency = RequiredField(fields, 4, path, line);
            if (currency != CrmDemoContract.ExportCurrency)
            {
                throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has a currency the import does not accept.");
            }

            orders.Add(new DemoOrderRecord(
                RequiredField(fields, 0, path, line),
                RequiredField(fields, 1, path, line),
                ParseInstant(RequiredField(fields, 2, path, line), path, line),
                ParseMoney(RequiredField(fields, 3, path, line), path, line),
                ParseInt(RequiredField(fields, 5, path, line), path, line),
                ParseFlag(RequiredField(fields, 6, path, line), path, line)));
        }

        return orders;
    }

    private static IEnumerable<(string[] Fields, int Line)> ReadRows(string path, IReadOnlyList<string> expectedColumns)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);

        var header = reader.ReadLine();
        if (header is null || !header.Split(DemoSeedConstants.CsvSeparator).SequenceEqual(expectedColumns))
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} has an unexpected header.");
        }

        var line = 1;
        while (reader.ReadLine() is { } text)
        {
            line++;
            if (text.Length == 0)
            {
                continue;
            }

            var fields = text.Split(DemoSeedConstants.CsvSeparator);
            if (fields.Length != expectedColumns.Count)
            {
                throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has {fields.Length} fields, expected {expectedColumns.Count}.");
            }

            yield return (fields, line);
        }
    }

    private static string RequiredField(string[] fields, int index, string path, int line)
    {
        if (fields[index].Length == 0)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has an empty required field.");
        }

        return fields[index];
    }

    private static DateTime ParseInstant(string text, string path, int line) =>
        DateTime.TryParseExact(text, "yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var instant)
            ? DateTime.SpecifyKind(instant, DateTimeKind.Utc)
            : throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has an invalid instant.");

    private static decimal ParseMoney(string text, string path, int line) =>
        decimal.TryParse(text, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount)
            ? amount
            : throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has an invalid amount.");

    private static int ParseInt(string text, string path, int line) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has an invalid count.");

    private static bool ParseFlag(string text, string path, int line) => text switch
    {
        "1" => true,
        "0" => false,
        _ => throw new InvalidDataException($"{Path.GetFileName(path)} line {line} has an invalid flag."),
    };
}
