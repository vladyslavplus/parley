using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Infrastructure.Auth;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Helpers for the note endpoints and the timeline they feed.</summary>
public static class NoteApi
{
    private const string ItemsProperty = "items";
    private const string NextCursorProperty = "nextCursor";

    /// <summary>Creates a note on one subject; fails the test unless the response is 201.</summary>
    public static async Task<JsonElement> CreateAsync(
        HttpClient client,
        string body,
        Guid? contactId,
        Guid? dealId,
        Guid? companyId,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(CrmRoutes.Notes, new { body, contactId, dealId, companyId }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>Reads one page of the timeline of one subject, or of the note list when <paramref name="route"/> says so.</summary>
    public static async Task<JsonElement> PageAsync(HttpClient client, string route, string query, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{route}?{query}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>Items of a page, in page order.</summary>
    public static IEnumerable<JsonElement> ItemsOf(JsonElement page) => page.GetProperty(ItemsProperty).EnumerateArray();

    /// <summary>The cursor of the next page, or null on the last page.</summary>
    public static string? NextCursorOf(JsonElement page) =>
        page.GetProperty(NextCursorProperty).ValueKind == JsonValueKind.Null ? null : page.GetProperty(NextCursorProperty).GetString();

    public static Guid IdOf(JsonElement note) => note.GetProperty("id").GetGuid();

    public static uint VersionOf(JsonElement note) => note.GetProperty("version").GetUInt32();
}
