namespace Parley.Crm.Contracts.Errors;

/// <summary>
/// Stable error codes of the CRM module. Each code has an English and a Ukrainian text
/// in the shared error resources.
/// </summary>
public static class CrmErrorCodes
{
    public const string ContactNotFound = "crm.contact.not_found";
    public const string ContactEmailTaken = "crm.contact.email_taken";
    public const string ContactNotDeleted = "crm.contact.not_deleted";
    public const string ContactCompanyNotFound = "crm.contact.company_not_found";
    public const string ContactOwnerNotAllowed = "crm.contact.owner_not_allowed";
    public const string CompanyNotFound = "crm.company.not_found";
    public const string CompanyNotDeleted = "crm.company.not_deleted";
    public const string CompanyOwnerNotAllowed = "crm.company.owner_not_allowed";
    public const string TagNotFound = "crm.tag.not_found";
    public const string TagNameTaken = "crm.tag.name_taken";
    public const string CustomFieldNotFound = "crm.custom_field.not_found";
    public const string CustomFieldKeyTaken = "crm.custom_field.key_taken";
    public const string CustomFieldLimitReached = "crm.custom_field.limit_reached";
    public const string CustomFieldArchived = "crm.custom_field.archived";
    public const string CustomFieldUnknownKey = "crm.custom_field.unknown_key";
    public const string CustomFieldRequired = "crm.custom_field.required";
    public const string CustomFieldTypeMismatch = "crm.custom_field.type_mismatch";
    public const string CustomFieldValueInvalid = "crm.custom_field.value_invalid";
    public const string CustomFieldOptionInvalid = "crm.custom_field.option_invalid";
    public const string CustomFieldTextTooLong = "crm.custom_field.text_too_long";
    public const string ContactExportNotFound = "crm.export.not_found";
    public const string ContactExportNotReady = "crm.export.not_ready";
    public const string ContactExportExpired = "crm.export.expired";
    public const string ContactExportRangeInvalid = "crm.export.range_invalid";
    public const string ContactExportEnqueueFailed = "crm.export.enqueue_failed";
    public const string SearchQueryTooShort = "crm.search.query_too_short";
    public const string SearchQueryTooLong = "crm.search.query_too_long";
    public const string PipelineNotFound = "crm.pipeline.not_found";
    public const string StageNotFound = "crm.stage.not_found";
    public const string StageNameTaken = "crm.stage.name_taken";
    public const string StageLimitReached = "crm.stage.limit_reached";
    public const string StageHasDeals = "crm.stage.has_deals";
    public const string StageOpenRequired = "crm.stage.open_required";
    public const string StageTerminalFixed = "crm.stage.terminal_fixed";
    public const string StageOrderInvalid = "crm.stage.order_invalid";
    public const string DealNotFound = "crm.deal.not_found";
    public const string DealContactNotFound = "crm.deal.contact_not_found";
    public const string DealCompanyNotFound = "crm.deal.company_not_found";
    public const string DealOwnerNotAllowed = "crm.deal.owner_not_allowed";
    public const string DealStageNotFound = "crm.deal.stage_not_found";
    public const string DealNotDeleted = "crm.deal.not_deleted";
    public const string TaskNotFound = "crm.task.not_found";
    public const string TaskAssigneeNotAllowed = "crm.task.assignee_not_allowed";
    public const string TaskSubjectNotFound = "crm.task.subject_not_found";
    public const string NoteNotFound = "crm.note.not_found";
    public const string NoteSubjectRequired = "crm.note.subject_required";
    public const string NoteSubjectNotFound = "crm.note.subject_not_found";
    public const string ActivitySubjectRequired = "crm.activity.subject_required";
    public const string ActivitySubjectAmbiguous = "crm.activity.subject_ambiguous";
    public const string ActivitySubjectNotFound = "crm.activity.subject_not_found";
    public const string OrderNotFound = "crm.order.not_found";
}
