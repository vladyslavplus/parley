namespace Parley.Crm.Features.Orders.Shared;

/// <summary>Sort fields of the order list. Ties are broken by identifier.</summary>
public static class OrderSortFields
{
    /// <summary>Invoice instant. Default.</summary>
    public const string Ordered = "ordered";

    /// <summary>Net invoice total.</summary>
    public const string Amount = "amount";

    public static bool IsValid(string? value) =>
        value is null or Ordered or Amount;
}
