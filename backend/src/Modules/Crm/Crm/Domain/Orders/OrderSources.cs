namespace Parley.Crm.Domain.Orders;

/// <summary>Where an order came from. A new value is a code change.</summary>
public static class OrderSources
{
    /// <summary>Loaded by the demo seed from the cleaned public dataset.</summary>
    public const string DemoSeed = "demo_seed";

    /// <summary>Written by the demo campaign generator with an injected effect; labelled as simulated in the UI.</summary>
    public const string DemoSimulated = "demo_simulated";

    public const string Manual = "manual";
}
