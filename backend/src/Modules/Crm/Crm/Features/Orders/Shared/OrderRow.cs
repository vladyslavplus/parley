namespace Parley.Crm.Features.Orders.Shared;

/// <summary>Database shape of an order in a query result, before the view mapping.</summary>
public sealed class OrderRow
{
    public Guid Id { get; set; }

    public Guid ContactId { get; set; }

    public string ExternalRef { get; set; } = string.Empty;

    public DateTime OrderedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public int LineCount { get; set; }

    public bool IsCancellation { get; set; }

    public string Source { get; set; } = string.Empty;
}
