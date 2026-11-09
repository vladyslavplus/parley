namespace Parley.Crm.Domain.Deals;

/// <summary>Lifecycle of a deal. The status follows the kind of the deal's stage.</summary>
public static class DealStatuses
{
    public const string Open = "open";

    public const string Won = "won";

    public const string Lost = "lost";

    public static IReadOnlyList<string> All { get; } = [Open, Won, Lost];
}
