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

    public const string Pipelines = "pipelines";

    public const string PipelineById = "pipelines/{id:guid}";

    public const string PipelineBoard = "pipelines/{id:guid}/board";

    public const string PipelineStages = "pipelines/{id:guid}/stages";

    public const string PipelineStageById = "pipelines/{id:guid}/stages/{stageId:guid}";

    public const string PipelineStageOrder = "pipelines/{id:guid}/stages/order";

    public const string Deals = "deals";

    public const string DealById = "deals/{id:guid}";

    public const string DealMove = "deals/{id:guid}/move";

    public const string DealRestore = "deals/{id:guid}/restore";

    public const string Tasks = "tasks";

    public const string TasksMine = "tasks/mine";

    public const string TaskById = "tasks/{id:guid}";

    public const string TaskComplete = "tasks/{id:guid}/complete";

    public const string TaskReopen = "tasks/{id:guid}/reopen";

    public const string Notes = "notes";

    public const string NoteById = "notes/{id:guid}";

    public const string Activities = "activities";

    public const string Orders = "orders";

    public const string OrderById = "orders/{id:guid}";

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
