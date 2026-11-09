using Parley.SharedKernel.Persistence.Entities;

namespace Parley.Crm.Domain.Orders;

/// <summary>
/// An invoice of a contact. Amounts are net: a cancellation carries a negative total and
/// <see cref="IsCancellation"/> is set. The row is never edited; it feeds the customer metrics
/// reader and the campaign outcome reader.
/// </summary>
public sealed class Order : BusinessEntity
{
    public Guid ContactId { get; set; }

    /// <summary>Invoice number as the source names it; unique per organization and source.</summary>
    public string ExternalRef { get; set; } = string.Empty;

    /// <summary>Instant of the invoice, UTC.</summary>
    public DateTime OrderedAt { get; set; }

    public decimal TotalAmount { get; set; }

    /// <summary>ISO 4217 code, uppercase.</summary>
    public string Currency { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public bool IsCancellation { get; set; }

    /// <summary>One of <see cref="OrderSources"/>.</summary>
    public string Source { get; set; } = OrderSources.Manual;
}
