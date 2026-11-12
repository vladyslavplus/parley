namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>Names and table names of the generated demo seed. The volumes and shares live in <c>CrmDemoProfile</c>.</summary>
public static class DemoSeedConstants
{
    /// <summary>Reserved top-level domain: generated addresses can never reach a real mailbox.</summary>
    public const string EmailDomain = "demo.parley.test";

    /// <summary>Kind names of the identifiers derived from a contact number.</summary>
    public const string ContactKind = "contact";

    public const string CompanyKind = "company";

    public const string OrderKind = "order";

    public const string PipelineKind = "pipeline";

    public const string PipelineStageKind = "pipeline-stage";

    public const string TagKind = "tag";

    public const string ContactsStagingTable = "demo_seed_contacts";

    public const string OrdersStagingTable = "demo_seed_orders";
}
