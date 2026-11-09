namespace Parley.Campaigns.Features.Campaigns.Review;

/// <summary>
/// The experiment review. <paramref name="SrmPValue"/> is null until the audience exists; <paramref name="SrmWarning"/>
/// means the arm split contradicts the configured holdout share, so the lift cannot be trusted.
/// <paramref name="MinimumDetectableEffectBps"/> is the smallest targeted-arm difference the counts can detect, at the
/// preview baseline rate.
/// </summary>
public sealed record CampaignReviewView(
    Guid CampaignId,
    string Status,
    DateTime? AudienceFrozenAt,
    int HoldoutShareBps,
    CampaignArmSummary Treatment,
    CampaignArmSummary Holdout,
    double? SrmPValue,
    bool SrmWarning,
    int? MinimumDetectableEffectBps);
