namespace Parley.Campaigns.Features.Campaigns.Shared;

/// <summary>A campaign row of the campaign list: the fields the list and the status views need, without the holdout seed.</summary>
/// <param name="Id">Campaign identifier.</param>
/// <param name="Name">Campaign name.</param>
/// <param name="Status">Lifecycle state.</param>
/// <param name="HoldoutShareBps">Share kept out of the email, in basis points.</param>
/// <param name="TargetingMode">How the audience is targeted.</param>
/// <param name="AudienceFrozenAt">Set once the audience is frozen; null while the campaign is a draft.</param>
/// <param name="StartedAt">Set when the campaign started sending; null before that.</param>
/// <param name="CompletedAt">Set when every targeted recipient has an outcome; null before that.</param>
/// <param name="Version">Row version to send back on update.</param>
/// <param name="TemplateId">Template the campaign sends.</param>
/// <param name="SegmentId">Segment the audience comes from; null means every contact with an address.</param>
/// <param name="Source">Who created the campaign: <c>user</c> or <c>demo_seed</c>. A demo campaign is labeled as such before it completes.</param>
public sealed record CampaignSummaryView(
    Guid Id,
    string Name,
    string Status,
    int HoldoutShareBps,
    string TargetingMode,
    DateTime? AudienceFrozenAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    uint Version,
    Guid TemplateId,
    Guid? SegmentId,
    string Source);
