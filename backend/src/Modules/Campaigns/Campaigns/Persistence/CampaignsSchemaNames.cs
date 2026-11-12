namespace Parley.Campaigns.Persistence;

/// <summary>Schema, table and index names of the campaigns module. Mappings and migrations read the same values.</summary>
public static class CampaignsSchemaNames
{
    public const string Schema = "campaigns";

    public const string TemplatesTable = "templates";

    public const string SegmentsTable = "segments";

    public const string CampaignsTable = "campaigns";

    public const string RecipientsTable = "recipients";

    public const string SuppressionsTable = "suppressions";

    public const string SendRecordsTable = "send_records";

    public const string SendCampaignContactIndex = "ux_send_records_campaign_id_contact_id";

    public const string SendClaimIndex = "ix_send_records_organization_id_campaign_id_status";

    public const string SendStatusCheck = "ck_send_records_status";

    public const string RecipientOutcomesTable = "recipient_outcomes";

    public const string CampaignResultsTable = "campaign_results";

    public const string OutcomeCampaignRecipientIndex = "ux_recipient_outcomes_campaign_id_recipient_id";

    public const string ResultCampaignIndex = "ux_campaign_results_campaign_id";

    public const string TemplateListIndex = "ix_templates_organization_id_name_id";

    public const string SegmentListIndex = "ix_segments_organization_id_name_id";

    public const string CampaignListIndex = "ix_campaigns_organization_id_name_id";

    public const string RecipientArmIndex = "ix_recipients_organization_id_campaign_id_arm_is_targeted";

    public const string RecipientCampaignContactIndex = "ux_recipients_campaign_id_contact_id";

    public const string SuppressionEmailIndex = "ux_suppressions_organization_id_email_normalized";

    public const string SegmentDefinitionColumn = "definition";
}
