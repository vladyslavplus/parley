namespace Parley.Campaigns.Contracts.Errors;

/// <summary>
/// Stable error codes of the campaigns module. Each code has an English and a Ukrainian text
/// in the shared error resources.
/// </summary>
public static class CampaignsErrorCodes
{
    public const string TemplateNotFound = "campaigns.template.not_found";
    public const string TemplateSyntaxInvalid = "campaigns.template.syntax_invalid";
    public const string TemplateVariableUnknown = "campaigns.template.variable_unknown";
    public const string TemplateTagNotAllowed = "campaigns.template.tag_not_allowed";
    public const string TemplateFilterNotAllowed = "campaigns.template.filter_not_allowed";
    public const string TemplateTooLong = "campaigns.template.too_long";
    public const string TemplateRenderFailed = "campaigns.template.render_failed";
    public const string TemplateVersionConflict = "campaigns.template.version_conflict";
    public const string SegmentNotFound = "campaigns.segment.not_found";
    public const string SegmentRfmNotAvailable = "campaigns.segment.rfm_not_available";
    public const string SegmentVersionConflict = "campaigns.segment.version_conflict";
    public const string TestSendRateLimited = "campaigns.test_send.rate_limited";
    public const string CampaignNotFound = "campaigns.campaign.not_found";
    public const string CampaignAudienceFrozen = "campaigns.campaign.audience_frozen";
    public const string CampaignNotDraft = "campaigns.campaign.not_draft";
    public const string CampaignInvalidState = "campaigns.campaign.invalid_state";
    public const string CampaignVersionConflict = "campaigns.campaign.version_conflict";
    public const string CampaignHoldoutShareOutOfRange = "campaigns.campaign.holdout_share_out_of_range";
    public const string CampaignTargetingShareOutOfRange = "campaigns.campaign.targeting_share_out_of_range";
    public const string CampaignTargetingModeInvalid = "campaigns.campaign.targeting_mode_invalid";
    public const string CampaignTargetingNotPermitted = "campaigns.campaign.targeting_not_permitted";
    public const string CampaignScoresUnavailable = "campaigns.campaign.scores_unavailable";
    public const string SendMailUnavailable = "campaigns.send.mail_unavailable";
    public const string SendResultUnknown = "campaigns.send.result_unknown";
    public const string SendRecipientUnavailable = "campaigns.send.recipient_unavailable";
    public const string SendTestFailed = "campaigns.send.test_failed";
    public const string SendTestAddressMissing = "campaigns.send.test_address_missing";
    public const string UnsubscribeTokenInvalid = "campaigns.unsubscribe.token_invalid";
    public const string UnsubscribeTokenExpired = "campaigns.unsubscribe.token_expired";
}
