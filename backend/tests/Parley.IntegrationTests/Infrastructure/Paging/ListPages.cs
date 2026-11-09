using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Parley.IntegrationTests.Infrastructure.Auth;

namespace Parley.IntegrationTests.Infrastructure.Paging;

/// <summary>Reads every page of a keyset-paged list by following the next cursor, the way a client does.</summary>
public static class ListPages
{
    private const string ItemsProperty = "items";
    private const string NextCursorProperty = "nextCursor";
    private const string AfterParameter = "after";
    private const char QueryStart = '?';
    private const string QueryJoin = "&";
    private const string QueryStartText = "?";

    public static async Task<List<JsonElement>> ReadAllAsync(HttpClient client, string route, CancellationToken cancellationToken)
    {
        var items = new List<JsonElement>();
        string? cursor = null;
        do
        {
            var separator = route.Contains(QueryStart) ? QueryJoin : QueryStartText;
            var url = cursor is null
                ? route
                : $"{route}{separator}{AfterParameter}={Uri.EscapeDataString(cursor)}";
            var response = await client.GetAsync(url, cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
            items.AddRange(page.GetProperty(ItemsProperty).EnumerateArray());
            cursor = page.TryGetProperty(NextCursorProperty, out var next) && next.ValueKind == JsonValueKind.String
                ? next.GetString()
                : null;
        }
        while (cursor is not null);

        return items;
    }
}
