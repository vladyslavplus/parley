using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Identity.Teams;

/// <summary>The team list searches by name, sorts by name or creation with a stable cursor, and hides other organizations.</summary>
public sealed class TeamListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Route = "teams";
    private const string NameProperty = "name";

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
    public async Task TeamList_SearchesSortsAndPages_WithinOneOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await CreateTeamAsync(admin, "Sales EU", token);
        await CreateTeamAsync(admin, "Sales US", token);
        await CreateTeamAsync(admin, "Support", token);
        var otherOrganization = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var otherAdmin = CrmApi.Client(_factory, otherOrganization.AccessToken);
        await CreateTeamAsync(otherAdmin, "Sales Other Organization", token);

        (await NamesAsync(admin, $"{Route}?q=sales", token)).Should().Equal("Sales EU", "Sales US");
        (await NamesAsync(admin, $"{Route}?sort=createdAt&order=desc&limit=1", token, paged: true))
            .Should().Equal("Support", "Sales US", "Sales EU");
        (await NamesAsync(admin, $"{Route}?sort=name&order=desc&limit=2", token, paged: true))
            .Should().Equal("Support", "Sales US", "Sales EU");
    }

    [Fact]
    public async Task TeamList_RejectsAnUnknownSortField_WithValidationCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var response = await admin.GetAsync($"{Route}?sort=members", token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, "sort", token)).Should().Be(ErrorCodes.ValidationFailed);
    }

    private static async Task CreateTeamAsync(HttpClient client, string name, CancellationToken token)
    {
        var response = await client.PostAsJsonAsync(IdentityRoutes.Teams, new { name }, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

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
