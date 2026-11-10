namespace Parley.Campaigns.Features.Campaigns.Results;

/// <summary>One arm: frozen recipients, targeted recipients, conversions, the conversion rate and the net revenue.</summary>
/// <param name="Assigned">Frozen recipients in the arm, targeted or not.</param>
/// <param name="Targeted">Targeted recipients in the arm: the population of the comparison.</param>
/// <param name="Converted">Converted targeted recipients.</param>
/// <param name="Rate">Conversion rate; null without targeted recipients.</param>
/// <param name="Revenue">
/// Net revenue of the targeted recipients in the window: the sum of the order totals in each order's stored currency, with no
/// conversion. The platform does not enforce one currency on orders, so a mixed-currency organization gets a mixed sum.
/// </param>
public sealed record CampaignResultsArmView(long Assigned, long Targeted, long Converted, double? Rate, decimal Revenue);
