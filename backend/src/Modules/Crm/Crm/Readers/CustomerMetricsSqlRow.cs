namespace Parley.Crm.Readers;

/// <summary>Unmapped row of the customer metrics statement; columns are aliased to these property names.</summary>
public sealed class CustomerMetricsSqlRow
{
    public Guid ContactId { get; set; }

    public Guid? OwnerUserId { get; set; }

    public DateTime? FirstOrderAt { get; set; }

    public DateTime? LastOrderAt { get; set; }

    public int OrderCount { get; set; }

    public int PurchaseDayCount { get; set; }

    public int OrdersShort { get; set; }

    public int OrdersLong { get; set; }

    public decimal TotalValue { get; set; }

    public decimal ValueLong { get; set; }

    public decimal AvgOrderValue { get; set; }

    public decimal AvgRepeatOrderValue { get; set; }

    public bool HasOpenDeal { get; set; }
}
