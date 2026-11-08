using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Domain.Roles;
using ContractPermissions = Parley.Identity.Contracts.Authorization.Permissions;

namespace Parley.Identity.Authorization.Seed;

/// <summary>
/// System roles and their permission grants, from the matrix in the functional
/// scope document. Used as the <c>HasData</c> source of the Identity migration, so
/// the seeded rows and the matrix cannot drift apart.
/// </summary>
public static class SystemRoleSeed
{
    /// <summary>Stable identifiers of the system roles; fixed because seeded rows are keyed on them.</summary>
    public static readonly Guid AdminRoleId = new("0192f5a0-7b1c-7000-8000-000000000001");

    public static readonly Guid SalesManagerRoleId = new("0192f5a0-7b1c-7000-8000-000000000002");

    public static readonly Guid SalesRepRoleId = new("0192f5a0-7b1c-7000-8000-000000000003");

    public static readonly Guid MarketerRoleId = new("0192f5a0-7b1c-7000-8000-000000000004");

    public static readonly Guid ViewerRoleId = new("0192f5a0-7b1c-7000-8000-000000000005");

    /// <summary>Role order used by every matrix row.</summary>
    private static readonly (Guid Id, string Code)[] RoleColumns =
    [
        (AdminRoleId, SystemRoles.Admin),
        (SalesManagerRoleId, SystemRoles.SalesManager),
        (SalesRepRoleId, SystemRoles.SalesRep),
        (MarketerRoleId, SystemRoles.Marketer),
        (ViewerRoleId, SystemRoles.Viewer),
    ];

    private static readonly MatrixRow[] Matrix =
    [
        new(ContractPermissions.ContactsRead, Org, Team, Own, Org, Team),
        new(ContractPermissions.ContactsWrite, Org, Team, Own, Org, None),
        new(ContractPermissions.ContactsDelete, Org, Team, None, None, None),
        new(ContractPermissions.ContactsExport, Org, Team, None, Org, None),
        new(ContractPermissions.CompaniesRead, Org, Team, Own, Org, Team),
        new(ContractPermissions.CompaniesWrite, Org, Team, Own, Org, None),
        new(ContractPermissions.CompaniesDelete, Org, Team, None, None, None),
        new(ContractPermissions.DealsRead, Org, Team, Own, Org, Team),
        new(ContractPermissions.DealsWrite, Org, Team, Own, None, None),
        new(ContractPermissions.DealsDelete, Org, Team, None, None, None),
        new(ContractPermissions.TasksRead, Org, Team, Own, Own, Team),
        new(ContractPermissions.TasksWrite, Org, Team, Own, Own, None),
        new(ContractPermissions.TasksDelete, Org, Team, Own, Own, None),
        new(ContractPermissions.ActivitiesRead, Org, Team, Own, Org, Team),
        new(ContractPermissions.ActivitiesWrite, Org, Team, Own, Org, None),
        new(ContractPermissions.OrdersRead, Org, Team, Own, Org, Team),
        new(ContractPermissions.PipelinesManage, Plain, None, None, None, None),
        new(ContractPermissions.CustomFieldsManage, Plain, None, None, None, None),
        new(ContractPermissions.TagsManage, Plain, Plain, None, Plain, None),
        new(ContractPermissions.CampaignsRead, Plain, Plain, None, Plain, Plain),
        new(ContractPermissions.CampaignsManage, Plain, None, None, Plain, None),
        new(ContractPermissions.CampaignsSend, Plain, None, None, Plain, None),
        new(ContractPermissions.DashboardsView, Plain, Plain, Plain, Plain, Plain),
        new(ContractPermissions.InsightsView, Plain, Plain, Plain, Plain, Plain),
        new(ContractPermissions.InsightsMlView, Plain, Plain, None, Plain, None),
        new(ContractPermissions.InsightsMlRun, Plain, None, None, Plain, None),
        new(ContractPermissions.InsightsMlManage, Plain, None, None, None, None),
        new(ContractPermissions.UsersManage, Plain, None, None, None, None),
        new(ContractPermissions.TeamsManage, Plain, None, None, None, None),
        new(ContractPermissions.OrgManage, Plain, None, None, None, None),
        new(ContractPermissions.AuditRead, Plain, None, None, None, None),
    ];

    private const GrantLevel None = GrantLevel.NotGranted;
    private const GrantLevel Plain = GrantLevel.OrganizationLevel;
    private const GrantLevel Own = GrantLevel.Own;
    private const GrantLevel Team = GrantLevel.Team;
    private const GrantLevel Org = GrantLevel.Organization;

    /// <summary>The five system roles.</summary>
    public static IReadOnlyList<Role> Roles { get; } =
        RoleColumns.Select(column => new Role
        {
            Id = column.Id,
            OrganizationId = null,
            Code = column.Code,
            IsSystem = true,
        }).ToArray();

    /// <summary>Every granted permission of every system role, derived from the matrix.</summary>
    public static IReadOnlyList<RolePermission> RolePermissions { get; } = BuildGrants();

    /// <summary>Stored scope text for a grant level; null for organization-level and not-granted.</summary>
    public static string? ScopeText(GrantLevel level) => level switch
    {
        GrantLevel.Own => RoleScopeNames.Own,
        GrantLevel.Team => RoleScopeNames.Team,
        GrantLevel.Organization => RoleScopeNames.Organization,
        _ => null,
    };

    private static RolePermission[] BuildGrants()
    {
        var grants = new List<RolePermission>();
        foreach (var row in Matrix)
        {
            for (var column = 0; column < RoleColumns.Length; column++)
            {
                var level = row.Levels[column];
                if (level == GrantLevel.NotGranted)
                {
                    continue;
                }

                grants.Add(new RolePermission
                {
                    RoleId = RoleColumns[column].Id,
                    Permission = row.Permission,
                    Scope = ScopeText(level),
                });
            }
        }

        return grants.ToArray();
    }

    private sealed record MatrixRow(string Permission, params GrantLevel[] Levels);
}
