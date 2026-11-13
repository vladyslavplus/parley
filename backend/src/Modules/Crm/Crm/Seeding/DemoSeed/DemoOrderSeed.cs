namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>One generated demo invoice. Cancellations carry a negative amount.</summary>
public sealed record DemoOrderSeed(
    Guid Id,
    string InvoiceNo,
    string CustomerRef,
    DateTime OrderedAt,
    decimal TotalAmount,
    int LineCount,
    bool IsCancellation);
