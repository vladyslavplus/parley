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
/// Stage settings keep the pipeline valid: names are unique, a stage holding deals cannot go, won and lost stay,
/// the open stages keep their order, and the last open stage cannot be removed.
/// </summary>
public sealed class StageSettingsTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string DemoStageName = "Demo";
    private const string ChangedStageName = "demo";
    private const int DemoColorIndex = 2;
    private const int DemoProbabilityBps = 3000;

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
    public async Task CreateAndReorder_KeepOpenStagesBeforeWonAndLost_AndNamesUnique()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);

        var created = await admin.PostAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStages, pipelineId),
            new { name = DemoStageName, colorIndex = DemoColorIndex, probabilityBps = DemoProbabilityBps },
            token);
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var stages = PipelineApi.ParseStages(await AuthTestClient.ReadJsonAsync(created, token));
        stages.Select(stage => stage.Name).Should().Equal("New", "Qualified", "Proposal", "Negotiation", DemoStageName, "Won", "Lost");

        var duplicate = await admin.PostAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStages, pipelineId),
            new { name = ChangedStageName, colorIndex = 0, probabilityBps = 0 },
            token);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(duplicate, token)).Should().Be(CrmErrorCodes.StageNameTaken);

        var demo = PipelineApi.Named(stages, DemoStageName);
        var reordered = await admin.PutAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStageOrder, pipelineId),
            new { stageIds = new[] { demo.Id, PipelineApi.Named(stages, "New").Id, PipelineApi.Named(stages, "Qualified").Id, PipelineApi.Named(stages, "Proposal").Id, PipelineApi.Named(stages, "Negotiation").Id } },
            token);
        reordered.StatusCode.Should().Be(HttpStatusCode.OK);
        PipelineApi.ParseStages(await AuthTestClient.ReadJsonAsync(reordered, token))
            .Select(stage => stage.Name)
            .Should().Equal(DemoStageName, "New", "Qualified", "Proposal", "Negotiation", "Won", "Lost");

        var incomplete = await admin.PutAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStageOrder, pipelineId),
            new { stageIds = new[] { demo.Id } },
            token);
        incomplete.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(incomplete, "stageIds", token)).Should().Be(CrmErrorCodes.StageOrderInvalid);
    }

    [Fact]
    public async Task Removal_RefusesStagesWithDeals_WonAndLost_AndTheLastOpenStage()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var stages = await PipelineApi.StagesAsync(admin, pipelineId, token);
        var newStage = PipelineApi.Named(stages, "New");
        var won = PipelineApi.Named(stages, "Won");

        var created = await admin.PostAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStages, pipelineId),
            new { name = DemoStageName, colorIndex = DemoColorIndex, probabilityBps = DemoProbabilityBps },
            token);
        var demo = PipelineApi.Named(PipelineApi.ParseStages(await AuthTestClient.ReadJsonAsync(created, token)), DemoStageName);
        var deal = await DealApi.CreateAsync(admin, "Blocks removal", 100m, demo.Id, null, token);

        var blocked = await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, demo.Id), token);
        blocked.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(blocked, token)).Should().Be(CrmErrorCodes.StageHasDeals);

        var terminal = await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, won.Id), token);
        terminal.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(terminal, token)).Should().Be(CrmErrorCodes.StageTerminalFixed);

        var moved = await DealApi.MoveAsync(admin, DealApi.IdOf(deal), won.Id, 0, DealApi.VersionOf(deal), token);
        moved.StatusCode.Should().Be(HttpStatusCode.OK);
        foreach (var stage in new[] { demo, PipelineApi.Named(stages, "Qualified"), PipelineApi.Named(stages, "Proposal"), PipelineApi.Named(stages, "Negotiation") })
        {
            (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, stage.Id), token))
                .StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        var lastOpen = await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, newStage.Id), token);
        lastOpen.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(lastOpen, token)).Should().Be(CrmErrorCodes.StageOpenRequired);
    }
}
