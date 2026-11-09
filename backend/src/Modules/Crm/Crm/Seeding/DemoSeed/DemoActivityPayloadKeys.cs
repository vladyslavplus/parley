namespace Parley.Crm.Seeding.DemoSeed;

/// <summary>
/// Field names of the activity payloads the demo seed writes. They match the payloads the live handlers write
/// (deal moves, deal closures, orders), so the timeline renders seeded and live entries the same way.
/// </summary>
public static class DemoActivityPayloadKeys
{
    public const string InvoiceNo = "invoiceNo";

    public const string TotalAmount = "totalAmount";

    public const string Currency = "currency";

    public const string IsCancellation = "isCancellation";

    public const string FromStageId = "fromStageId";

    public const string FromStageName = "fromStageName";

    public const string ToStageId = "toStageId";

    public const string ToStageName = "toStageName";

    public const string StageName = "stageName";

    public const string Status = "status";
}
