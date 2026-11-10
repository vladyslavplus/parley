namespace Parley.Campaigns.Features.Campaigns.Prepare;

/// <summary>The campaign settings the freeze reads, in one projection.</summary>
public sealed record CampaignFreezeInput(
    Guid Id,
    string Status,
    DateTime? PrepareAsOf,
    DateTime? AudienceFrozenAt,
    Guid? SegmentId,
    Guid TemplateId,
    string HoldoutSeed,
    int HoldoutShareBps,
    string TargetingMode,
    int? TargetingShareBps);
