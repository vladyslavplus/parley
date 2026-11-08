using System.Net;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Identity.Members;

/// <summary>
/// The member list searches by name and email, filters by role and status, sorts with a stable cursor, and
/// never shows members of another organization.
/// </summary>
public sealed class MemberListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NameProperty = "displayName";
    private const string OwnerName = "Zz Owner";
    private const int PageSize = 2;
    private const string Route = "members";

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
    public async Task MemberList_FiltersBySearchRoleAndStatus_WithinOneOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await AddNamedMemberAsync(owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, "Anna Kovalenko", token);
        var bohdan = await AddNamedMemberAsync(owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, "Bohdan Melnyk", token);
        await AddNamedMemberAsync(owner.OrganizationId, SystemRoleSeed.ViewerRoleId, "Maryna Koval", token);
        var otherOrganization = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, otherOrganization.UserId, "Anna Other Organization", token);

        (await NamesAsync(admin, $"{Route}?q=kov", token)).Should().Equal("Anna Kovalenko", "Maryna Koval");
        (await NamesAsync(admin, $"{Route}?q=anna", token)).Should().Equal("Anna Kovalenko");
        (await NamesAsync(admin, $"{Route}?roleCode=viewer&status=all", token)).Should().Equal("Maryna Koval");

        var membershipId = await MembershipTestData.ReadMembershipIdAsync(_factory.Services, owner.OrganizationId, bohdan.UserId, token);
        (await admin.PostAsync(IdentityApi.Route(IdentityRoutes.MemberDeactivate, membershipId), null, token))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await NamesAsync(admin, $"{Route}?q=bohdan", token)).Should().BeEmpty("a deactivated member is hidden by default");
        (await NamesAsync(admin, $"{Route}?q=bohdan&status=deactivated", token)).Should().Equal("Bohdan Melnyk");
        (await NamesAsync(admin, $"{Route}?q=bohdan&status=all", token)).Should().Equal("Bohdan Melnyk");
    }

    [Fact]
    public async Task MemberList_SortsByNameInBothOrders_AndPagesWithoutGapsOrDuplicates()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, owner.UserId, OwnerName, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        foreach (var name in new[] { "Anna", "Bohdan", "Chrystyna", "Dmytro", "Eva", "Eva" })
        {
            await AddNamedMemberAsync(owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, name, token);
        }

        var ascending = new[] { "Anna", "Bohdan", "Chrystyna", "Dmytro", "Eva", "Eva", OwnerName };
        (await NamesAsync(admin, $"{Route}?sort=name&order=asc&limit={PageSize}", token, paged: true)).Should().Equal(ascending);
        (await NamesAsync(admin, $"{Route}?sort=name&order=desc&limit={PageSize}", token, paged: true))
            .Should().Equal(ascending.Reverse());
    }

    [Fact]
    public async Task MemberList_RejectsAnUnknownSortField_WithValidationCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var response = await admin.GetAsync($"{Route}?sort=password", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, "sort", token)).Should().Be(ErrorCodes.ValidationFailed);
    }

    private async Task<(Guid UserId, string Email)> AddNamedMemberAsync(Guid organizationId, Guid roleId, string displayName, CancellationToken token)
    {
        var member = await IdentitySetup.AddMemberAsync(_factory, organizationId, roleId, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, member.UserId, displayName, token);
        return member;
    }

    /// <summary>Display names of the rows of a list. A single page unless <paramref name="paged"/> follows every cursor.</summary>
    private static async Task<List<string?>> NamesAsync(HttpClient client, string route, CancellationToken token, bool paged = false)
    {
        if (paged)
        {
            return (await ListPages.ReadAllAsync(client, route, token))
                .Select(row => row.GetProperty(NameProperty).GetString())
                .ToList();
        }

        var response = await client.GetAsync(route, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, token);
        return page.GetProperty("items").EnumerateArray()
            .Select(row => row.GetProperty(NameProperty).GetString())
            .ToList();
    }
}
