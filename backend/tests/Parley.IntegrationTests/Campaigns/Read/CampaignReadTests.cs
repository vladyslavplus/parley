using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Read;

/// <summary>
/// The campaign list and the single campaign read: keyset paging in both sort orders, the status and text filters, the
/// validation answer, and isolation between organizations. One host serves the whole class, so the test runs once.
/// </summary>
public sealed class CampaignReadTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const string CampaignsRoute = "campaigns";

    private const string ItemName = "name";

    private const string ItemId = "id";

    private const string ItemVersion = "version";

    private const string ItemTemplateId = "templateId";

    private const string ItemStatus = "status";

    private const string ProblemCodeProperty = "code";

    private const string CampaignTargetingAll = CampaignTargetingModes.All;

    private static readonly string[] Names = ["Alpha", "Bravo", "Charlie", "Delta", "Echo"];

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
    public async Task CampaignList_PagesFiltersAndGets_AndHidesOtherOrganizations()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);

        var created = new Dictionary<string, (Guid Id, uint Version)>(StringComparer.Ordinal);
        foreach (var name in Names)
        {
            created[name] = await CreateNamedCampaignAsync(admin, name, templateId, token);
        }

        // Paging: every page follows the cursor, and the names come back in name order without a gap or a repeat.
        var pagedNames = (await ListPages.ReadAllAsync(admin, CampaignsRoute + "?limit=2", token))
            .Select(item => item.GetProperty(ItemName).GetString()).ToArray();
        pagedNames.Should().Equal(Names);

        var firstPage = await admin.GetAsync(CampaignsRoute + "?limit=2", token);
        firstPage.StatusCode.Should().Be(HttpStatusCode.OK);
        (await AuthTestClient.ReadJsonAsync(firstPage, token)).GetProperty("items").GetArrayLength().Should().Be(2);

        var descending = await ListPages.ReadAllAsync(admin, CampaignsRoute + "?order=desc", token);
        descending.First().GetProperty(ItemName).GetString().Should().Be("Echo");

        var newestFirst = await ListPages.ReadAllAsync(admin, CampaignsRoute + "?sort=created&order=desc&limit=2", token);
        newestFirst.Select(item => item.GetProperty(ItemName).GetString()).Should().Equal(["Echo", "Delta", "Charlie", "Bravo", "Alpha"]);

        // Filters: the text narrows by name, the status by state.
        var byText = await ListPages.ReadAllAsync(admin, CampaignsRoute + "?q=cha", token);
        byText.Select(item => item.GetProperty(ItemName).GetString()).Should().Equal("Charlie");

        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, created["Alpha"].Id, token);
        var ready = await ListPages.ReadAllAsync(admin, CampaignsRoute + "?status=ready", token);
        ready.Select(item => item.GetProperty(ItemName).GetString()).Should().Equal("Alpha");
        var drafts = await ListPages.ReadAllAsync(admin, CampaignsRoute + "?status=draft", token);
        drafts.Should().HaveCount(Names.Length - 1);

        var unknownStatus = await admin.GetAsync(CampaignsRoute + "?status=paused", token);
        unknownStatus.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthTestClient.ReadJsonAsync(unknownStatus, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(ErrorCodes.ValidationFailed);

        // Get: one campaign with the version the update answers with.
        var single = await admin.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignById, created["Alpha"].Id), token);
        single.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await AuthTestClient.ReadJsonAsync(single, token);
        view.GetProperty(ItemName).GetString().Should().Be("Alpha");
        view.GetProperty(ItemStatus).GetString().Should().Be(CampaignStatuses.Ready);
        view.GetProperty(ItemTemplateId).GetGuid().Should().Be(templateId);
        // Preparing wrote the row again, so the version moved on from the one create answered with.
        view.GetProperty(ItemVersion).GetUInt32().Should().NotBe(created["Alpha"].Version);

        // Isolation: a second organization sees none of these campaigns, and a direct read answers as an unknown id.
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var stranger = CrmApi.Client(_factory, other.AccessToken);
        var strangerList = await ListPages.ReadAllAsync(stranger, CampaignsRoute, token);
        strangerList.Should().BeEmpty();

        var strangerGet = await stranger.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignById, created["Alpha"].Id), token);
        strangerGet.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await AuthTestClient.ReadJsonAsync(strangerGet, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.CampaignNotFound);

        var unknown = await admin.GetAsync(IdentityApi.Route(CampaignsRoutes.CampaignById, Guid.CreateVersion7()), token);
        unknown.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<(Guid Id, uint Version)> CreateNamedCampaignAsync(HttpClient admin, string name, Guid templateId, CancellationToken cancellationToken)
    {
        var response = await admin.PostAsJsonAsync(
            CampaignsRoutes.Campaigns,
            new { name, templateId, holdoutShareBps = HoldoutShareBps, targetingMode = CampaignTargetingAll },
            cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var view = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return (view.GetProperty(ItemId).GetGuid(), view.GetProperty(ItemVersion).GetUInt32());
    }
}
