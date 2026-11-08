using System.Net;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// Keyset paging returns every row exactly once even when the sort values tie, and the list
/// reads its rows in a single statement.
/// </summary>
public sealed class ContactPagingTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int ContactCount = 5;
    private const int PageSize = 2;
    private const string ItemsProperty = "items";
    private const string CursorProperty = "nextCursor";
    private const string SortByCreated = "created";
    private const string SortAscending = "asc";
    private const string SortDescending = "desc";
    private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);
    private const string ContactsTableReference = "crm.contacts";

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
    public async Task TiedSortValues_ReturnEachContactOnce_AcrossPages()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        // The fake clock does not move, so every contact has the same creation instant and the same name.
        var created = new List<Guid>();
        for (var index = 0; index < ContactCount; index++)
        {
            var body = ContactFaker.Create() with { FirstName = "Same", LastName = "Name" };
            created.Add(CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, body, token)));
        }

        foreach (var sort in new[] { SortByCreated, null })
        {
            var seen = await ReadAllPagesAsync(admin, sort, null, token);
            seen.Should().HaveCount(ContactCount);
            seen.Distinct().Should().HaveCount(ContactCount);
            seen.Should().BeEquivalentTo(created);
        }
    }

    [Fact]
    public async Task TiedSortValues_Descending_ReturnEachContactOnce_AcrossPages()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var created = new List<Guid>();
        for (var index = 0; index < ContactCount; index++)
        {
            var body = ContactFaker.Create() with { FirstName = "Same", LastName = "Name" };
            created.Add(CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, body, token)));
        }

        foreach (var sort in new[] { SortByCreated, null })
        {
            var seen = await ReadAllPagesAsync(admin, sort, SortDescending, token);
            seen.Should().HaveCount(ContactCount);
            seen.Distinct().Should().HaveCount(ContactCount);
            seen.Should().BeEquivalentTo(created);
        }
    }

    [Fact]
    public async Task CreatedSort_PagesInCreationOrder_WhenInstantsDiffer()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var created = new List<Guid>();
        for (var index = 0; index < ContactCount; index++)
        {
            _factory.Clock.Advance(OneSecond);
            created.Add(CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token)));
        }

        (await ReadAllPagesAsync(admin, SortByCreated, SortAscending, token)).Should().Equal(created);
        (await ReadAllPagesAsync(admin, SortByCreated, SortDescending, token)).Should().Equal(Enumerable.Reverse(created));
    }

    [Fact]
    public async Task ContactList_ReadsRowsInASingleStatement()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token);
        }

        using var recorder = new SqlStatementRecorder();
        var response = await admin.GetAsync($"{CrmRoutes.Contacts}?limit={ContactCount}", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        recorder.Statements.Count(statement => statement.Contains(ContactsTableReference, StringComparison.Ordinal))
            .Should().Be(1);
    }

    private static async Task<List<Guid>> ReadAllPagesAsync(HttpClient client, string? sort, string? order, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        string? cursor = null;
        do
        {
            var query = $"{CrmRoutes.Contacts}?limit={PageSize}"
                + (sort is null ? string.Empty : $"&sort={sort}")
                + (order is null ? string.Empty : $"&order={order}")
                + (cursor is null ? string.Empty : $"&after={Uri.EscapeDataString(cursor)}");
            var response = await client.GetAsync(query, cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
            ids.AddRange(page.GetProperty(ItemsProperty).EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
            cursor = page.TryGetProperty(CursorProperty, out var next) && next.ValueKind == System.Text.Json.JsonValueKind.String
                ? next.GetString()
                : null;
        }
        while (cursor is not null);

        return ids;
    }
}
