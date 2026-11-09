using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Infrastructure.Auth;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Helpers for the deal endpoints: create, move, read and list identifiers.</summary>
public static class DealApi
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string VersionProperty = "version";
    private const string DefaultCurrency = "UAH";

    /// <summary>Creates a deal; fails the test unless the response is 201.</summary>
    public static async Task<JsonElement> CreateAsync(
        HttpClient client,
        string title,
        decimal amount,
        Guid? stageId,
        Guid? ownerUserId,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            CrmRoutes.Deals,
            new { title, amount, currency = DefaultCurrency, stageId, ownerUserId },
            cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    public static Task<HttpResponseMessage> MoveAsync(
        HttpClient client,
        Guid dealId,
        Guid stageId,
        int position,
        uint version,
        CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(PipelineApi.Route(CrmRoutes.DealMove, dealId), new { stageId, position, version }, cancellationToken);

    public static async Task<JsonElement> ReadAsync(HttpClient client, Guid dealId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(PipelineApi.Route(CrmRoutes.DealById, dealId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>Identifiers of one page of the deal list, in list order.</summary>
    public static async Task<List<Guid>> ListIdsAsync(HttpClient client, string query, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{CrmRoutes.Deals}?{query}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray().Select(item => item.GetProperty(IdProperty).GetGuid()).ToList();
    }

    public static Guid IdOf(JsonElement deal) => deal.GetProperty(IdProperty).GetGuid();

    public static uint VersionOf(JsonElement deal) => deal.GetProperty(VersionProperty).GetUInt32();
}
