namespace Parley.Crm.Contracts.Audit;

/// <summary>
/// Action codes of CRM audit events. Rows carry the code and the record identifier,
/// never personal payloads.
/// </summary>
public static class CrmAuditActions
{
    public const string ContactDeleted = "crm.contact.deleted";

    public const string ContactRestored = "crm.contact.restored";

    public const string CompanyDeleted = "crm.company.deleted";

    public const string CompanyRestored = "crm.company.restored";

    public const string ContactsExported = "crm.contacts.exported";
}
