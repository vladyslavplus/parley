namespace Parley.Api.Controllers.Crm;

/// <summary>Route templates of the CRM endpoints.</summary>
public static class CrmRoutes
{
    public const string Contacts = "contacts";

    public const string ContactById = "contacts/{id:guid}";

    public const string ContactRestore = "contacts/{id:guid}/restore";

    public const string ContactsBulkOwner = "contacts/bulk-owner";

    public const string ContactsBulkDelete = "contacts/bulk-delete";

    public const string ContactTags = "contacts/{id:guid}/tags";

    public const string ContactTag = "contacts/{id:guid}/tags/{tagId:guid}";

    public const string ContactExports = "contacts/exports";

    public const string ContactExportById = "contacts/exports/{id:guid}";

    public const string ContactExportDownload = "contacts/exports/{id:guid}/download";

    public const string Search = "search";

    public const string Companies = "companies";

    public const string CompanyById = "companies/{id:guid}";

    public const string CompanyRestore = "companies/{id:guid}/restore";

    public const string CompaniesBulkOwner = "companies/bulk-owner";

    public const string CompaniesBulkDelete = "companies/bulk-delete";

    public const string Tags = "tags";

    public const string TagById = "tags/{id:guid}";

    public const string Owners = "owners";

    public const string CustomFields = "custom-fields";

    public const string CustomFieldById = "custom-fields/{id:guid}";

    public const string CustomFieldArchive = "custom-fields/{id:guid}/archive";
}
