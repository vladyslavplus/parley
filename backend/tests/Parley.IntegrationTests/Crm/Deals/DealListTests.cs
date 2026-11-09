using System.Net;
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

/// <summary>The deal list pages through every deal once, in the requested order, with filters and an invalid-sort refusal.</summary>
public sealed class DealListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";
    private const string NextCursorProperty = "nextCursor";
    private const string AmountProperty = "amount";
    private const string IdProperty = "id";
    private const string PageLimit = "2";
    private const string NewStageName = "New";
    private const string QualifiedStageName = "Qualified";
    private const string WonStageName = "Won";

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
    public async Task AmountSort_PagesEveryDealOnce_InOrder_WhenAmountsTie()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var newStageId = PipelineApi.Named(await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token), NewStageName).Id;
        foreach (var amount in new[] { 10m, 30m, 30m, 20m, 50m })
        {
            await DealApi.CreateAsync(admin, $"Amount {amount}", amount, newStageId, null, token);
        }

        var seen = new List<(Guid Id, decimal Amount)>();
        string? cursor = null;
        do
        {
            var query = $"limit={PageLimit}&sort=amount&order=desc" + (cursor is null ? string.Empty : $"&after={Uri.EscapeDataString(cursor)}");
            var response = await admin.GetAsync($"{CrmRoutes.Deals}?{query}", token);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await AuthTestClient.ReadJsonAsync(response, token);
            seen.AddRange(page.GetProperty(ItemsProperty).EnumerateArray()
                .Select(item => (item.GetProperty(IdProperty).GetGuid(), item.GetProperty(AmountProperty).GetDecimal())));
            cursor = page.TryGetProperty(NextCursorProperty, out var next) && next.ValueKind == JsonValueKind.String ? next.GetString() : null;
        }
        while (cursor is not null);

        seen.Should().HaveCount(5);
        seen.Select(entry => entry.Id).Distinct().Should().HaveCount(5);
        seen.Select(entry => entry.Amount).Should().Equal(50m, 30m, 30m, 20m, 10m);
    }

    [Fact]
    public async Task Filters_NarrowByStatusStageAndText_AndAnUnknownSortIsRefused()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var newStageId = PipelineApi.Named(stages, NewStageName).Id;
        var qualifiedStageId = PipelineApi.Named(stages, QualifiedStageName).Id;
        var wonStageId = PipelineApi.Named(stages, WonStageName).Id;
        var alpha = await DealApi.CreateAsync(admin, "Alpha renewal", 10m, newStageId, null, token);
        var beta = await DealApi.CreateAsync(admin, "Beta upsell", 20m, qualifiedStageId, null, token);
        await DealApi.MoveAsync(admin, DealApi.IdOf(beta), wonStageId, 0, DealApi.VersionOf(beta), token);

        (await DealApi.ListIdsAsync(admin, "status=won", token)).Should().ContainSingle(id => id == DealApi.IdOf(beta));
        (await DealApi.ListIdsAsync(admin, $"stageId={newStageId:D}", token)).Should().Equal(DealApi.IdOf(alpha));
        (await DealApi.ListIdsAsync(admin, "q=alpha", token)).Should().Equal(DealApi.IdOf(alpha));

        var refused = await admin.GetAsync($"{CrmRoutes.Deals}?sort=unknown", token);
        refused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
