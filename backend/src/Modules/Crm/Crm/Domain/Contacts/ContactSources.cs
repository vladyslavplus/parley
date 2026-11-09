namespace Parley.Crm.Domain.Contacts;

/// <summary>Where a contact came from. A new value is a code change.</summary>
public static class ContactSources
{
    public const string Manual = "manual";

    /// <summary>Loaded by the demo seed from the cleaned public dataset.</summary>
    public const string DemoSeed = "demo_seed";
}
