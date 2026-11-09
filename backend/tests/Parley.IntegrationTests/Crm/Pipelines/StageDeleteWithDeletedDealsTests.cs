using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Pipelines;

/// <summary>
/// Deleting a stage ignores soft-deleted deals: they move to the first other open stage, so restoring one lands on a
/// live stage. A live deal on the stage still blocks the delete.
/// </summary>
public sealed class StageDeleteWithDeletedDealsTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string DemoStageName = "Archive demo";
    private const string FirstOpenStageName = "New";
    private const int DemoColorIndex = 1;
    private const int DemoProbabilityBps = 2000;

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
    public async Task DeletedDealsOnTheStage_MoveToTheFirstOpenStage_AndTheStageGoes()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var demo = await CreateStageAsync(admin, pipelineId, token);
        var deal = await DealApi.CreateAsync(admin, "Archived deal", 10m, demo.Id, null, token);
        var dealId = DealApi.IdOf(deal);
        (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.DealById, dealId), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var removed = await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, demo.Id), token);
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var restored = await admin.PostAsync(PipelineApi.Route(CrmRoutes.DealRestore, dealId), null, token);
        restored.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await AuthTestClient.ReadJsonAsync(restored, token);
        view.GetProperty("stageName").GetString().Should().Be(FirstOpenStageName);
    }

    [Fact]
    public async Task LiveDealOnTheStage_StillBlocksTheDelete()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var demo = await CreateStageAsync(admin, pipelineId, token);
        await DealApi.CreateAsync(admin, "Live deal", 10m, demo.Id, null, token);

        var blocked = await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, demo.Id), token);

        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(blocked, token)).Should().Be(CrmErrorCodes.StageHasDeals);
    }

    private static async Task<PipelineApi.Stage> CreateStageAsync(HttpClient client, Guid pipelineId, CancellationToken cancellationToken)
    {
        var created = await client.PostAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStages, pipelineId),
            new { name = DemoStageName, colorIndex = DemoColorIndex, probabilityBps = DemoProbabilityBps },
            cancellationToken);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        return PipelineApi.Named(PipelineApi.ParseStages(await AuthTestClient.ReadJsonAsync(created, cancellationToken)), DemoStageName);
    }
}
