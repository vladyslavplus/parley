namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>Names, file layout and generation settings of the demo seed. The seed reads the export written by the ML module.</summary>
public static class DemoSeedConstants
{
    public const string ContactsFileName = "contacts.csv.gz";

    public const string OrdersFileName = "orders.csv.gz";

    /// <summary>Column names expected in the contacts file, in order.</summary>
    public static IReadOnlyList<string> ContactColumns { get; } = ["customer_id", "country_iso2"];

    /// <summary>Column names expected in the orders file, in order.</summary>
    public static IReadOnlyList<string> OrderColumns { get; } =
        ["invoice_no", "customer_id", "ordered_at", "total_amount", "currency", "line_count", "is_cancellation"];


    /// <summary>Reserved top-level domain: generated addresses can never reach a real mailbox.</summary>
    public const string EmailDomain = "demo.parley.test";

    /// <summary>Prefix of the generated address local part, followed by the source customer id.</summary>
    public const string EmailLocalPrefix = "customer-";

    /// <summary>Locale of the generated names, phones and job titles.</summary>
    public const string FakerLocale = "en";

    /// <summary>Kind names of the identifiers derived from a customer id.</summary>
    public const string ContactKind = "contact";

    public const string CompanyKind = "company";

    public const string OrderKind = "order";

    public const string PipelineKind = "pipeline";

    public const string PipelineStageKind = "pipeline-stage";

    public const string ContactsStagingTable = "demo_seed_contacts";

    public const string OrdersStagingTable = "demo_seed_orders";

    public const string UtcZoneName = "UTC";

    /// <summary>Column count of one row of each file.</summary>
    public static int ContactColumnCount => ContactColumns.Count;

    public static int OrderColumnCount => OrderColumns.Count;

    public const char CsvSeparator = ',';
}
