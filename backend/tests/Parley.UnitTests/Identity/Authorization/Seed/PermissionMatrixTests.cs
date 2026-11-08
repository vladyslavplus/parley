using AwesomeAssertions;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Xunit;

namespace Parley.UnitTests.Identity.Authorization.Seed;

/// <summary>
/// The seeded system roles and grants must equal the permission matrix of the
/// functional scope document (section 5.3), row by row.
/// </summary>
public sealed class PermissionMatrixTests
{
    // Restated from 07 section 5.3, columns admin, sales_manager, sales_rep, marketer, viewer.
    // O = organization scope, T = team scope, W = own scope, Y = organization-level grant, - = not granted.
    private static readonly (string Permission, string Admin, string SalesManager, string SalesRep, string Marketer, string Viewer)[] Matrix =
    [
        (Permissions.ContactsRead, "O", "T", "W", "O", "T"),
        (Permissions.ContactsWrite, "O", "T", "W", "O", "-"),
        (Permissions.ContactsDelete, "O", "T", "-", "-", "-"),
        (Permissions.ContactsExport, "O", "T", "-", "O", "-"),
        (Permissions.CompaniesRead, "O", "T", "W", "O", "T"),
        (Permissions.CompaniesWrite, "O", "T", "W", "O", "-"),
        (Permissions.CompaniesDelete, "O", "T", "-", "-", "-"),
        (Permissions.DealsRead, "O", "T", "W", "O", "T"),
        (Permissions.DealsWrite, "O", "T", "W", "-", "-"),
        (Permissions.DealsDelete, "O", "T", "-", "-", "-"),
        (Permissions.TasksRead, "O", "T", "W", "W", "T"),
        (Permissions.TasksWrite, "O", "T", "W", "W", "-"),
        (Permissions.TasksDelete, "O", "T", "W", "W", "-"),
        (Permissions.ActivitiesRead, "O", "T", "W", "O", "T"),
        (Permissions.ActivitiesWrite, "O", "T", "W", "O", "-"),
        (Permissions.OrdersRead, "O", "T", "W", "O", "T"),
        (Permissions.PipelinesManage, "Y", "-", "-", "-", "-"),
        (Permissions.CustomFieldsManage, "Y", "-", "-", "-", "-"),
        (Permissions.TagsManage, "Y", "Y", "-", "Y", "-"),
        (Permissions.CampaignsRead, "Y", "Y", "-", "Y", "Y"),
        (Permissions.CampaignsManage, "Y", "-", "-", "Y", "-"),
        (Permissions.CampaignsSend, "Y", "-", "-", "Y", "-"),
        (Permissions.DashboardsView, "Y", "Y", "Y", "Y", "Y"),
        (Permissions.InsightsView, "Y", "Y", "Y", "Y", "Y"),
        (Permissions.InsightsMlView, "Y", "Y", "-", "Y", "-"),
        (Permissions.InsightsMlRun, "Y", "-", "-", "Y", "-"),
        (Permissions.InsightsMlManage, "Y", "-", "-", "-", "-"),
        (Permissions.UsersManage, "Y", "-", "-", "-", "-"),
        (Permissions.TeamsManage, "Y", "-", "-", "-", "-"),
        (Permissions.OrgManage, "Y", "-", "-", "-", "-"),
        (Permissions.AuditRead, "Y", "-", "-", "-", "-"),
    ];

    [Fact]
    public void SeededRolesAndGrants_EqualFunctionalScopeMatrix_PermissionMatrix()
    {
        string[] roleCodes = [SystemRoles.Admin, SystemRoles.SalesManager, SystemRoles.SalesRep, SystemRoles.Marketer, SystemRoles.Viewer];

        var expected = roleCodes.ToDictionary(code => code, _ => new Dictionary<string, string?>(StringComparer.Ordinal));
        foreach (var row in Matrix)
        {
            string[] cells = [row.Admin, row.SalesManager, row.SalesRep, row.Marketer, row.Viewer];
            for (var column = 0; column < roleCodes.Length; column++)
            {
                var (granted, scope) = ReadCell(cells[column]);
                if (granted)
                {
                    expected[roleCodes[column]][row.Permission] = scope;
                }
            }
        }

        var roleCodeById = SystemRoleSeed.Roles.ToDictionary(role => role.Id, role => role.Code);
        var actual = roleCodes.ToDictionary(code => code, _ => new Dictionary<string, string?>(StringComparer.Ordinal));
        foreach (var grant in SystemRoleSeed.RolePermissions)
        {
            actual[roleCodeById[grant.RoleId]][grant.Permission] = grant.Scope;
        }

        actual.Should().BeEquivalentTo(expected);
        Permissions.All.Should().BeEquivalentTo(Matrix.Select(row => row.Permission));
    }

    private static (bool Granted, string? Scope) ReadCell(string cell) => cell switch
    {
        "O" => (true, RoleScopeNames.Organization),
        "T" => (true, RoleScopeNames.Team),
        "W" => (true, RoleScopeNames.Own),
        "Y" => (true, null),
        "-" => (false, null),
        _ => throw new InvalidOperationException($"Unknown matrix cell '{cell}'."),
    };
}
