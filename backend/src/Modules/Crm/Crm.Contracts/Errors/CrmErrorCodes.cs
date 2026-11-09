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
}
