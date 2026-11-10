namespace Parley.Campaigns.Features.Campaigns.Shared;

/// <summary>A campaign as the builder shows it.</summary>
/// <param name="Id">Campaign identifier.</param>
/// <param name="Name">Name shown in the campaign list.</param>
/// <param name="TemplateId">Template the campaign sends.</param>
/// <param name="SegmentId">Segment the audience comes from; null means every contact with an address.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="HoldoutShareBps">Share kept out of the email, in basis points.</param>
/// <param name="HoldoutSeed">Seed of the holdout assignment; fixed at creation.</param>
/// <param name="TargetingMode">How the audience is targeted.</param>
/// <param name="TargetingShareBps">Share a score ranking targets; null when every recipient is targeted.</param>
/// <param name="AudienceFrozenAt">Set once the audience is frozen; the audience is immutable from then on.</param>
/// <param name="Version">Row version to send back on update; a changed row answers 409.</param>
/// <param name="Source">Who created the campaign: <c>user</c> or <c>demo_seed</c>. A demo campaign is labeled as such before it completes.</param>
public sealed record CampaignView(
    Guid Id,
    string Name,
    Guid TemplateId,
    Guid? SegmentId,
    string Status,
    int HoldoutShareBps,
    string HoldoutSeed,
    string TargetingMode,
    int? TargetingShareBps,
    DateTime? AudienceFrozenAt,
    uint Version,
    string Source);
