using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Xunit;

namespace Parley.IntegrationTests.Crm.Tags;

/// <summary>The tag list searches names case-insensitively and pages in name order.</summary>
public sealed class TagListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NameProperty = "name";
    private const short ColorIndex = 1;

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
    public async Task TagList_SearchesByNameCaseInsensitively_AndPagesInNameOrder()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        foreach (var name in new[] { "Priority", "Prospect", "Partner" })
        {
            (await admin.PostAsJsonAsync(CrmRoutes.Tags, new { name, colorIndex = ColorIndex }, token))
                .StatusCode.Should().Be(HttpStatusCode.Created);
        }

        (await NamesAsync(admin, $"{CrmRoutes.Tags}?q=PR", token)).Should().Equal("Priority", "Prospect");
        (await NamesAsync(admin, $"{CrmRoutes.Tags}?q=par&limit=1", token)).Should().Equal("Partner");
        (await ListPages.ReadAllAsync(admin, $"{CrmRoutes.Tags}?limit=2", token))
            .Select(row => row.GetProperty(NameProperty).GetString())
            .Should().Equal("Partner", "Priority", "Prospect");
    }

    private static async Task<List<string?>> NamesAsync(HttpClient client, string route, CancellationToken token)
    {
        var response = await client.GetAsync(route, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, token);
        return page.GetProperty("items").EnumerateArray().Select(row => row.GetProperty(NameProperty).GetString()).ToList();
    }
}
