namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// The permission catalogue. Format is resource.action in lowercase; ML permissions
/// use the insights.ml prefix. Scope is not part of the string: it is an attribute
/// of the role-permission grant (see <see cref="RoleScopeNames"/>).
/// </summary>
public static class Permissions
{
    public const string ContactsRead = "contacts.read";
    public const string ContactsWrite = "contacts.write";
    public const string ContactsDelete = "contacts.delete";
    public const string ContactsExport = "contacts.export";
    public const string CompaniesRead = "companies.read";
    public const string CompaniesWrite = "companies.write";
    public const string CompaniesDelete = "companies.delete";
    public const string DealsRead = "deals.read";
    public const string DealsWrite = "deals.write";
    public const string DealsDelete = "deals.delete";
    public const string TasksRead = "tasks.read";
    public const string TasksWrite = "tasks.write";
    public const string TasksDelete = "tasks.delete";
    public const string ActivitiesRead = "activities.read";
    public const string ActivitiesWrite = "activities.write";
    public const string OrdersRead = "orders.read";
    public const string PipelinesManage = "pipelines.manage";
    public const string CustomFieldsManage = "customfields.manage";
    public const string TagsManage = "tags.manage";
    public const string CampaignsRead = "campaigns.read";
    public const string CampaignsManage = "campaigns.manage";
    public const string CampaignsSend = "campaigns.send";
    public const string DashboardsView = "dashboards.view";
    public const string InsightsView = "insights.view";
    public const string InsightsMlView = "insights.ml.view";
    public const string InsightsMlRun = "insights.ml.run";
    public const string InsightsMlManage = "insights.ml.manage";
    public const string UsersManage = "users.manage";
    public const string TeamsManage = "teams.manage";
    public const string OrgManage = "org.manage";
    public const string AuditRead = "audit.read";

    /// <summary>Every permission in the catalogue, in matrix order.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        ContactsRead, ContactsWrite, ContactsDelete, ContactsExport,
        CompaniesRead, CompaniesWrite, CompaniesDelete,
        DealsRead, DealsWrite, DealsDelete,
        TasksRead, TasksWrite, TasksDelete,
        ActivitiesRead, ActivitiesWrite,
        OrdersRead,
        PipelinesManage, CustomFieldsManage, TagsManage,
        CampaignsRead, CampaignsManage, CampaignsSend,
        DashboardsView, InsightsView, InsightsMlView, InsightsMlRun, InsightsMlManage,
        UsersManage, TeamsManage, OrgManage, AuditRead,
    ];
}
