namespace Parley.Api.Controllers.Campaigns.Requests;

/// <summary>
/// Body of a campaign create or update request. Missing required fields fail validation; the holdout share and the
/// targeting share fall back to the configured defaults when omitted.
/// </summary>
/// <param name="Name">Campaign name.</param>
/// <param name="TemplateId">Template to send.</param>
/// <param name="SegmentId">Segment of the audience; omit for every contact with an address.</param>
/// <param name="HoldoutShareBps">Holdout share in basis points; omit for the default.</param>
/// <param name="TargetingMode"><c>all</c>, <c>top_uplift</c> or <c>top_rfm</c>; omit for <c>all</c>.</param>
/// <param name="TargetingShareBps">Share a ranking targets; required for ranking modes.</param>
/// <param name="Version">Row version the client read; required on update, ignored on create.</param>
public sealed record CampaignRequest(
    string? Name,
    Guid? TemplateId,
    Guid? SegmentId,
    int? HoldoutShareBps,
    string? TargetingMode,
    int? TargetingShareBps,
    uint? Version);
