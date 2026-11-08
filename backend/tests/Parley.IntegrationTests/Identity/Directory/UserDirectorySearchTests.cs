using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Users;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Identity.Directory;

/// <summary>The directory search returns active members of the organization only, and honors the id limit.</summary>
public sealed class UserDirectorySearchTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Term = "olena";
    private const int Limit = 20;
    private const string ActiveName = "Olena Active";
    private const string DeactivatedName = "Olena Deactivated";
    private const string OtherOrganizationName = "Olena Other Organization";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task SearchActiveMembers_ReturnsActiveMembersOfTheOrganizationOnly()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, owner.UserId, ActiveName, token);
        var deactivated = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, deactivated.UserId, DeactivatedName, token);
        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, deactivated.UserId, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token))
            .StatusCode.Should().Be(System.Net.HttpStatusCode.NoContent);
        var otherOrganization = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, otherOrganization.UserId, OtherOrganizationName, token);

        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(owner.OrganizationId);
        var directory = scope.ServiceProvider.GetRequiredService<IUserDirectory>();

        var found = await directory.SearchActiveMembersAsync(owner.OrganizationId, Term, null, Limit, token);
        found.Select(entry => entry.DisplayName).Should().Equal(ActiveName);
        found.Single().IsActive.Should().BeTrue();

        var byId = await directory.SearchActiveMembersAsync(owner.OrganizationId, null, [deactivated.UserId], Limit, token);
        byId.Should().BeEmpty("a deactivated member is not found by search");

        (await directory.SearchActiveMembersAsync(owner.OrganizationId, Term, [], Limit, token)).Should().BeEmpty();
    }
}
