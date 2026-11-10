namespace Parley.Campaigns.Features.Campaigns.Review;

/// <summary>Recipient counts of one arm.</summary>
/// <param name="Arm">Arm name.</param>
/// <param name="Count">Recipients in the arm, targeted or not.</param>
/// <param name="TargetedCount">Recipients in the arm that the ranking targeted.</param>
public sealed record CampaignArmSummary(string Arm, int Count, int TargetedCount);
