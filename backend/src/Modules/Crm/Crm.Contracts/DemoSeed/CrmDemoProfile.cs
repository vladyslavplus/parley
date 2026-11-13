namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>
/// Volumes and shares of the generated demo workspace. Every value has a documented default; the Development seed can
/// override them through <c>Demo:Seed:Profile</c>, and the tests use a small profile. The archetype mix lives with the
/// generator because it describes the purchase patterns, not a deployment setting.
/// </summary>
public sealed class CrmDemoProfile
{
    /// <summary>Contacts in the generated workspace.</summary>
    public int Contacts { get; set; } = 5000;

    /// <summary>Months of order history, ending yesterday.</summary>
    public int OrderMonths { get; set; } = 24;

    /// <summary>Share of contacts that belong to a company.</summary>
    public int CompanySharePercent { get; set; } = 30;

    /// <summary>Share of recently active contacts that get an open, won or lost deal.</summary>
    public int DealSharePercent { get; set; } = 55;

    /// <summary>Share of contacts that get a task.</summary>
    public int TaskSharePercent { get; set; } = 40;

    /// <summary>Share of contacts that get a note.</summary>
    public int NoteSharePercent { get; set; } = 22;

    /// <summary>Share of contacts that get at least one tag.</summary>
    public int TagSharePercent { get; set; } = 45;

    /// <summary>Share of contacts with no order at all, so the no-orders segment is populated.</summary>
    public int NoOrdersPercent { get; set; } = 5;

    /// <summary>Share of orders written as cancellations. Only contacts with enough orders receive them.</summary>
    public int CancelledOrderPercent { get; set; } = 2;
}
