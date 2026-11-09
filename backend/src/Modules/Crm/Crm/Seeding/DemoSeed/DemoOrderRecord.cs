namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>One invoice of the export. Amounts are net; cancellations are negative.</summary>
/// <param name="InvoiceNo">Invoice number as the source names it.</param>
/// <param name="CustomerRef">Source customer id of the invoice.</param>
/// <param name="OrderedAt">Invoice instant as written in the export, UTC, before the date shift.</param>
/// <param name="TotalAmount">Net total of the invoice.</param>
/// <param name="LineCount">Lines of the invoice.</param>
/// <param name="IsCancellation">True for cancellation invoices.</param>
public sealed record DemoOrderRecord(
    string InvoiceNo,
    string CustomerRef,
    DateTime OrderedAt,
    decimal TotalAmount,
    int LineCount,
    bool IsCancellation);
