using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Contracts.Authorization;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Identity.Invitations;

/// <summary>The pending invitation list searches by email, filters by role, and sorts with a stable cursor.</summary>
public sealed class InvitationListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Route = "invitations";
    private const string EmailProperty = "email";
    private const string FirstEmail = "reviewer.one@contacts.parley.test";
    private const string SecondEmail = "reviewer.two@contacts.parley.test";
    private const string ThirdEmail = "stranger@contacts.parley.test";

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
    public async Task InvitationList_SearchesFiltersAndSorts_NewestFirstByDefault()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await Invite(admin, FirstEmail, SystemRoleSeed.SalesRepRoleId, token);
        await Invite(admin, SecondEmail, SystemRoleSeed.ViewerRoleId, token);
        await Invite(admin, ThirdEmail, SystemRoleSeed.ViewerRoleId, token);

        (await EmailsAsync(admin, Route, token)).Should().Equal(ThirdEmail, SecondEmail, FirstEmail);
        (await EmailsAsync(admin, $"{Route}?q=reviewer", token)).Should().Equal(SecondEmail, FirstEmail);
        (await EmailsAsync(admin, $"{Route}?roleCode=viewer", token)).Should().Equal(ThirdEmail, SecondEmail);
        (await EmailsAsync(admin, $"{Route}?sort=email&order=asc&limit=1", token, paged: true))
            .Should().Equal(FirstEmail, SecondEmail, ThirdEmail);
    }

    [Fact]
    public async Task InvitationList_RejectsAnUnknownSortField_WithValidationCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var response = await admin.GetAsync($"{Route}?sort=token", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, "sort", token)).Should().Be(ErrorCodes.ValidationFailed);
    }

    private static async Task Invite(HttpClient admin, string email, Guid roleId, CancellationToken token)
    {
        var roleCode = roleId == SystemRoleSeed.ViewerRoleId ? SystemRoles.Viewer : SystemRoles.SalesRep;
        (await admin.PostAsJsonAsync(IdentityRoutes.Invitations, new { email, roleCode }, token)).EnsureSuccessStatusCode();
    }

    private static async Task<List<string?>> EmailsAsync(HttpClient client, string route, CancellationToken token, bool paged = false)
    {
        if (paged)
        {
            return (await ListPages.ReadAllAsync(client, route, token))
                .Select(row => row.GetProperty(EmailProperty).GetString())
                .ToList();
        }

        var response = await client.GetAsync(route, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, token);
        return page.GetProperty("items").EnumerateArray()
            .Select(row => row.GetProperty(EmailProperty).GetString())
            .ToList();
    }
}
