namespace Parley.Crm.Contracts.DemoSeed;

/// <summary>Values the demo export and its consumers share. Changing one is a contract change for every host of the seed.</summary>
public static class CrmDemoContract
{
    /// <summary>Currency the demo export is written in. The import refuses export files in any other currency.</summary>
    public const string ExportCurrency = "GBP";
}
