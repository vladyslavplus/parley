namespace Parley.Crm.Persistence;

/// <summary>
/// Schema, table, column, index and constraint names of the CRM module. Mappings and
/// migrations read the same values.
/// </summary>
public static class CrmSchemaNames
{
    public const string Schema = "crm";

    public const string TrigramExtension = "pg_trgm";

    public const string ContactsTable = "contacts";

    public const string CompaniesTable = "companies";

    public const string TagsTable = "tags";

    public const string ContactTagsTable = "contact_tags";

    public const string ContactTagPrimaryKey = "pk_contact_tags";

    public const string CustomFieldDefinitionsTable = "custom_field_definitions";

    public const string ActivitiesTable = "activities";

    public const string ContactExportsTable = "contact_exports";

    public const string ContactExportRequesterIndex = "ix_contact_exports_organization_id_requested_by_created";

    public const string ContactExportExpiresIndex = "ix_contact_exports_expires_at";

    public const string DeletedAtColumn = "deleted_at";

    public const string EmailNormalizedColumn = "email_normalized";

    public const string SearchVectorColumn = "search_vector";

    /// <summary>Name of the shadow property that maps <see cref="SearchVectorColumn"/>.</summary>
    public const string SearchVectorProperty = "SearchVector";

    public const string GinMethod = "gin";

    public const string TrigramOperatorClass = "gin_trgm_ops";

    public const string JsonPathOperatorClass = "jsonb_path_ops";

    public const string ColorCheckConstraint = "ck_tags_color_index";

    public const string ContactEmailIndex = "ux_contacts_organization_id_email_normalized_live";

    public const string ContactIdIndex = "ix_contacts_organization_id_id";

    public const string ContactOwnerIndex = "ix_contacts_organization_id_owner_user_id_id";

    public const string ContactCreatedIndex = "ix_contacts_organization_id_created_at_id";

    public const string ContactNameIndex = "ix_contacts_organization_id_name_normalized_id";

    public const string ContactCompanyIndex = "ix_contacts_organization_id_company_id_id";

    public const string ContactSearchIndex = "ix_contacts_search_vector";

    public const string ContactNameTrigramIndex = "ix_contacts_name_normalized_trgm";

    public const string ContactEmailTrigramIndex = "ix_contacts_email_normalized_trgm";

    public const string ContactCustomFieldsIndex = "ix_contacts_custom_fields";

    public const string CompanyIdIndex = "ix_companies_organization_id_id";

    public const string CompanyOwnerIndex = "ix_companies_organization_id_owner_user_id_id";

    public const string CompanyCreatedIndex = "ix_companies_organization_id_created_at_id";

    public const string CompanyNameIndex = "ix_companies_organization_id_name_normalized_id";

    public const string CompanySearchIndex = "ix_companies_search_vector";

    public const string CompanyNameTrigramIndex = "ix_companies_name_normalized_trgm";

    public const string CompanyCustomFieldsIndex = "ix_companies_custom_fields";

    public const string TagNameIndex = "ux_tags_organization_id_name_normalized";

    public const string TagIdIndex = "ix_tags_organization_id_id";

    public const string ContactTagTagIndex = "ix_contact_tags_organization_id_tag_id";

    public const string CustomFieldKeyIndex = "ux_custom_field_definitions_organization_id_entity_type_key";

    public const string ActivityContactIndex = "ix_activities_organization_id_contact_id_occurred_at";

    public const string ActivitySourceEventIndex = "ux_activities_organization_id_source_event_id";
}
