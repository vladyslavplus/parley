namespace Parley.Api.Controllers.Campaigns;

/// <summary>Route templates of the campaigns endpoints.</summary>
public static class CampaignsRoutes
{
    public const string Templates = "templates";

    public const string TemplateById = "templates/{id:guid}";

    public const string TemplatePreview = "templates/{id:guid}/preview";

    public const string Segments = "segments";

    public const string SegmentById = "segments/{id:guid}";

    public const string SegmentPreview = "segments/preview";

    public const string Campaigns = "campaigns";

    public const string CampaignById = "campaigns/{id:guid}";

    public const string CampaignPrepare = "campaigns/{id:guid}/prepare";

    public const string CampaignReview = "campaigns/{id:guid}/review";
}
