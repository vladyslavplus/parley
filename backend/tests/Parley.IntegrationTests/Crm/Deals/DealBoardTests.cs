using System.Net;
using AwesomeAssertions;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Deals;

/// <summary>The board reports each stage's visible deal count and amount total, in a fixed number of statements.</summary>
public sealed class DealBoardTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NewStageName = "New";
    private const string QualifiedStageName = "Qualified";
    private const string WonStageName = "Won";
    private const string DealCountProperty = "dealCount";
    private const string TotalAmountProperty = "totalAmount";
    private const string StagesProperty = "stages";
    private const int ExtraDealCount = 10;

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
    public async Task Board_ReportsCountAndTotal_PerStage()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var stages = await PipelineApi.StagesAsync(admin, pipelineId, token);
        await DealApi.CreateAsync(admin, "First", 100m, PipelineApi.Named(stages, NewStageName).Id, null, token);
        await DealApi.CreateAsync(admin, "Second", 250.50m, PipelineApi.Named(stages, NewStageName).Id, null, token);
        await DealApi.CreateAsync(admin, "Third", 50m, PipelineApi.Named(stages, QualifiedStageName).Id, null, token);

        var board = await ReadBoardAsync(admin, pipelineId, null, token);

        var columns = board.GetProperty(StagesProperty).EnumerateArray().ToDictionary(stage => stage.GetProperty("name").GetString()!);
        columns[NewStageName].GetProperty(DealCountProperty).GetInt32().Should().Be(2);
        columns[NewStageName].GetProperty(TotalAmountProperty).GetDecimal().Should().Be(350.50m);
        columns[QualifiedStageName].GetProperty(DealCountProperty).GetInt32().Should().Be(1);
        columns[QualifiedStageName].GetProperty(TotalAmountProperty).GetDecimal().Should().Be(50m);
        columns[WonStageName].GetProperty(DealCountProperty).GetInt32().Should().Be(0);
        columns[WonStageName].GetProperty(TotalAmountProperty).GetDecimal().Should().Be(0m);
    }

    [Fact]
    public async Task Board_TotalsFollowTheDealFilters()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var newStageId = PipelineApi.Named(await PipelineApi.StagesAsync(admin, pipelineId, token), NewStageName).Id;
        await DealApi.CreateAsync(admin, "Alpha", 100m, newStageId, null, token);
        await DealApi.CreateAsync(admin, "Beta", 200m, newStageId, null, token);

        var unfiltered = await ReadBoardAsync(admin, pipelineId, null, token);
        var searched = await ReadBoardAsync(admin, pipelineId, "q=alpha", token);
        var won = await ReadBoardAsync(admin, pipelineId, "status=won", token);

        NewColumn(unfiltered).GetProperty(DealCountProperty).GetInt32().Should().Be(2);
        NewColumn(searched).GetProperty(DealCountProperty).GetInt32().Should().Be(1);
        NewColumn(searched).GetProperty(TotalAmountProperty).GetDecimal().Should().Be(100m);
        NewColumn(won).GetProperty(DealCountProperty).GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task Board_StatementCount_DoesNotGrowWithTheNumberOfDeals()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var newStageId = PipelineApi.Named(await PipelineApi.StagesAsync(admin, pipelineId, token), NewStageName).Id;
        await DealApi.CreateAsync(admin, "Only deal", 10m, newStageId, null, token);

        var fewStatements = await CountBoardStatementsAsync(admin, pipelineId, token);
        for (var index = 0; index < ExtraDealCount; index++)
        {
            await DealApi.CreateAsync(admin, $"Extra {index:D2}", 10m, newStageId, null, token);
        }

        var manyStatements = await CountBoardStatementsAsync(admin, pipelineId, token);
        manyStatements.Should().Be(fewStatements);
    }

    private static System.Text.Json.JsonElement NewColumn(System.Text.Json.JsonElement board) =>
        board.GetProperty(StagesProperty).EnumerateArray().Single(stage => stage.GetProperty("name").GetString() == NewStageName);

    private static async Task<System.Text.Json.JsonElement> ReadBoardAsync(
        HttpClient client,
        Guid pipelineId,
        string? query,
        CancellationToken cancellationToken)
    {
        var route = PipelineApi.Route(Parley.Api.Controllers.Crm.CrmRoutes.PipelineBoard, pipelineId);
        var response = await client.GetAsync(query is null ? route : $"{route}?{query}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<int> CountBoardStatementsAsync(HttpClient client, Guid pipelineId, CancellationToken cancellationToken)
    {
        using var recorder = new SqlStatementRecorder();
        await ReadBoardAsync(client, pipelineId, null, cancellationToken);
        return recorder.Statements.Count;
    }
}
