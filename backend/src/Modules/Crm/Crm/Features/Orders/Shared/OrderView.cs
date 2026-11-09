namespace Parley.Crm.Features.Orders.Shared;

/// <summary>An order as the API returns it. Amounts are net; a cancellation is negative and flagged.</summary>
/// <param name="Id">Order identifier.</param>
/// <param name="ContactId">Contact the order belongs to.</param>
/// <param name="ExternalRef">Invoice number.</param>
/// <param name="OrderedAt">Invoice instant, UTC.</param>
/// <param name="TotalAmount">Net total in <paramref name="Currency"/>.</param>
/// <param name="Currency">ISO 4217 code.</param>
/// <param name="LineCount">Lines of the invoice.</param>
/// <param name="IsCancellation">True for a cancellation.</param>
/// <param name="Source">Where the order came from.</param>
public sealed record OrderView(
    Guid Id,
    Guid ContactId,
    string ExternalRef,
    DateTimeOffset OrderedAt,
    decimal TotalAmount,
    string Currency,
    int LineCount,
    bool IsCancellation,
    string Source);
