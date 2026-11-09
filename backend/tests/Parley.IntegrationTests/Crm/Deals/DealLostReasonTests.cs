using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Deals;

/// <summary>
/// The lost reason follows the stage: a move that leaves the stage unchanged keeps it, entering the lost stage sets it,
/// and leaving the lost stage clears it. Timeline entries of a stage change name the stages.
/// </summary>
public sealed class DealLostReasonTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string Reason = "Price too high";
    private const string OpenStageName = "New";
    private const string WonStageName = "Won";
    private const string LostStageName = "Lost";

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
    public async Task LostReason_IsSetOnEntry_KeptWithinTheStage_AndClearedOnLeaving()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var deal = await DealApi.CreateAsync(admin, "Reason", 10m, PipelineApi.Named(stages, OpenStageName).Id, null, token);
        var dealId = DealApi.IdOf(deal);
        var lost = PipelineApi.Named(stages, LostStageName);

        var entered = await MoveWithReasonAsync(admin, dealId, lost.Id, DealApi.VersionOf(deal), Reason, token);
        entered.GetProperty("lostReason").GetString().Should().Be(Reason);

        var reordered = await MoveWithReasonAsync(admin, dealId, lost.Id, entered.GetProperty("version").GetUInt32(), null, token);
        reordered.GetProperty("lostReason").GetString().Should().Be(Reason, "a move within the lost stage keeps the reason");

        var left = await MoveWithReasonAsync(admin, dealId, PipelineApi.Named(stages, OpenStageName).Id, reordered.GetProperty("version").GetUInt32(), null, token);
        left.GetProperty("lostReason").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task StageChangeEntries_NameTheStages_AndKeepTheStableStatus()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var open = PipelineApi.Named(stages, OpenStageName);
        var won = PipelineApi.Named(stages, WonStageName);
        var deal = await DealApi.CreateAsync(admin, "Won deal", 10m, open.Id, null, token);
        var dealId = DealApi.IdOf(deal);

        var moved = await DealApi.MoveAsync(admin, dealId, won.Id, 0, DealApi.VersionOf(deal), token);
        moved.StatusCode.Should().Be(HttpStatusCode.OK);

        var entries = NoteApi.ItemsOf(await NoteApi.PageAsync(admin, CrmRoutes.Activities, $"dealId={dealId}", token)).ToList();
        var stageChanged = entries.Single(entry => entry.GetProperty("type").GetString() == "deal_stage_changed");
        stageChanged.GetProperty("payload").GetProperty("fromStageName").GetString().Should().Be(OpenStageName);
        stageChanged.GetProperty("payload").GetProperty("toStageName").GetString().Should().Be(WonStageName);
        stageChanged.GetProperty("payload").GetProperty("status").GetString().Should().Be("won");
        var closed = entries.Single(entry => entry.GetProperty("type").GetString() == "deal_closed");
        closed.GetProperty("payload").GetProperty("stageName").GetString().Should().Be(WonStageName);
        closed.GetProperty("payload").GetProperty("status").GetString().Should().Be("won");
    }

    /// <summary>Moves a deal with an optional lost reason; fails the test unless the move returns 200.</summary>
    private static async Task<JsonElement> MoveWithReasonAsync(
        HttpClient client,
        Guid dealId,
        Guid stageId,
        uint version,
        string? lostReason,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            PipelineApi.Route(CrmRoutes.DealMove, dealId),
            new { stageId, position = 0, version, lostReason },
            cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }
}
