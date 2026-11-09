using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Deals;

/// <summary>
/// Deals stay inside their organization, a viewer cannot write them, and a sales rep lists only the deals
/// they own.
/// </summary>
public sealed class DealAccessTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NewStageName = "New";

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
    public async Task OtherOrganization_CannotReadOrMoveTheDeal()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var deal = await DealApi.CreateAsync(admin, "Private", 10m, PipelineApi.Named(stages, NewStageName).Id, null, token);
        var stranger = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var other = CrmApi.Client(_factory, stranger.AccessToken);

        var read = await other.GetAsync(PipelineApi.Route(CrmRoutes.DealById, DealApi.IdOf(deal)), token);
        read.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var move = await DealApi.MoveAsync(other, DealApi.IdOf(deal), PipelineApi.Named(stages, NewStageName).Id, 0, DealApi.VersionOf(deal), token);
        move.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(move, token)).Should().Be(CrmErrorCodes.DealNotFound);
    }

    [Fact]
    public async Task Viewer_CannotCreateDeals_AndSalesRep_ListsOnlyOwnDeals()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var newStageId = PipelineApi.Named(await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token), NewStageName).Id;
        var viewer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.ViewerRoleId, token);
        using var viewerClient = CrmApi.Client(_factory, viewer.AccessToken);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var refused = await viewerClient.PostAsJsonAsync(
            CrmRoutes.Deals,
            new { title = "Not allowed", amount = 10m, currency = "UAH" },
            token);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var adminDeal = await DealApi.CreateAsync(admin, "Admin deal", 10m, newStageId, null, token);
        var repDeal = await DealApi.CreateAsync(repClient, "Rep deal", 20m, newStageId, null, token);

        (await DealApi.ListIdsAsync(repClient, "limit=100", token)).Should().Equal(DealApi.IdOf(repDeal));
        (await DealApi.ListIdsAsync(admin, "limit=100&sort=created", token))
            .Should().BeEquivalentTo(new[] { DealApi.IdOf(adminDeal), DealApi.IdOf(repDeal) });
    }
}
