namespace Parley.Campaigns.Domain.Campaigns;

/// <summary>What counts as a conversion for attribution.</summary>
public static class CampaignConversionRules
{
    public const string AnyOrder = "any_order";

    public const string OrderOrWonDeal = "order_or_won_deal";
}
