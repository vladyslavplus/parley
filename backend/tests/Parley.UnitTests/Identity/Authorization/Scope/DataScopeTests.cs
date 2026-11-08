using AwesomeAssertions;
using Parley.Identity.Authorization.Scope;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Xunit;

namespace Parley.UnitTests.Identity.Authorization.Scope;

/// <summary>
/// Record visibility per system role, derived from the seeded contacts.read grants.
/// </summary>
public sealed class DataScopeTests
{
    [Theory]
    [InlineData(SystemRoles.Admin, true, DataScopeLevel.Organization)]
    [InlineData(SystemRoles.SalesManager, true, DataScopeLevel.Team)]
    [InlineData(SystemRoles.SalesManager, false, DataScopeLevel.Own)]
    [InlineData(SystemRoles.SalesRep, true, DataScopeLevel.Own)]
    [InlineData(SystemRoles.Marketer, true, DataScopeLevel.Organization)]
    [InlineData(SystemRoles.Viewer, true, DataScopeLevel.Team)]
    public void DataScope_ResolvesPerSystemRole_ForContactsRead(string roleCode, bool userHasTeam, DataScopeLevel expected)
    {
        var roleId = SystemRoleSeed.Roles.Single(role => role.Code == roleCode).Id;
        var grant = SystemRoleSeed.RolePermissions.Single(
            candidate => candidate.RoleId == roleId && candidate.Permission == Permissions.ContactsRead);

        DataScopeRules.Resolve(grant.Scope, userHasTeam).Should().Be(expected);
    }

    [Fact]
    public void DataScope_UnknownStoredScope_SeesNothing()
    {
        DataScopeRules.Resolve("everything", userHasTeam: true).Should().Be(DataScopeLevel.None);
    }
}
