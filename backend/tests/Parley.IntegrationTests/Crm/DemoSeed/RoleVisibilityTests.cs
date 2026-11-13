using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Domain.Orders;
using Parley.Identity.Contracts.DemoSeed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Crm.DemoSeed;

/// <summary>
/// Each demo account sees the share the role matrix gives it, measured through the API: admin and marketer see every
/// record, a manager and a viewer see the records of their team, and a rep sees only its own.
/// </summary>
public sealed class RoleVisibilityTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int CustomerCount = 60;

    private const string SharedPassword = "test1234";

    private const int PageSize = 25;

    private const string ItemsProperty = "items";

    private const string NextCursorProperty = "nextCursor";

    private const string ContactsPath = "contacts";

    private const string DealsPath = "deals";

    private const string OrdersPath = "orders";

    private const string ManagerNorth = "manager.north@";

    private const string RepNorthFirst = "rep.north.1@";

    private const string RepSouthFirst = "rep.south.1@";

    private const string ViewerEmailPrefix = "viewer@";

    private const string MarketerEmailPrefix = "marketer@";

    private const string AdminEmailPrefix = "admin@";

    private IdentityApiFactory _factory = null!;

    private IReadOnlyList<DemoAccount> _accounts = [];

    private Guid _organizationId;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);

        var token = TestContext.Current.CancellationToken;
        var organizationId = Guid.CreateVersion7();
        using var scope = _factory.Services.CreateScope();
        var profile = new DemoOrganizationProfile(
            organizationId,
            "Parley Demo",
            "parley-demo-" + organizationId.ToString("N")[..8],
            "Europe/Kyiv",
            CultureCodes.Ukrainian,
            CrmDemoContract.DemoCurrency);
        var identity = await scope.ServiceProvider.GetRequiredService<IIdentityDemoSeeder>()
            .EnsureAsync(profile, SharedPassword, token);
        _accounts = identity.Accounts;

        var owners = _accounts.Where(account => account.RoleCode == "sales_rep").Select(account => account.UserId).ToArray();
        var crmProfile = new CrmDemoProfile
        {
            Contacts = CustomerCount,
            OrderMonths = 12,
            DealSharePercent = 60,
            TaskSharePercent = 40,
            NoteSharePercent = 25,
            TagSharePercent = 45,
            NoOrdersPercent = 0,
        };
        await scope.ServiceProvider.GetRequiredService<ICrmDemoSeeder>().SeedAsync(
            new CrmDemoImportRequest(organizationId, CrmDemoContract.DemoCurrency, CultureCodes.Ukrainian, owners, 7, _factory.Clock.GetUtcNow(), crmProfile),
            token);
        _organizationId = organizationId;
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task EachRoleSeesTheShareTheRoleMatrixGives()
    {
        var token = TestContext.Current.CancellationToken;
        var contactOwners = await OwnerCountsAsync("contacts", ContactSources.DemoSeed, token);
        var dealOwners = await OwnerCountsAsync("deals", DealSources.DemoSeed, token);
        var orderOwners = await OrderOwnerCountsAsync(token);
        var totalContacts = contactOwners.Values.Sum();
        var totalDeals = dealOwners.Values.Sum();
        var totalOrders = orderOwners.Values.Sum();
        totalContacts.Should().Be(CustomerCount);
        totalDeals.Should().BeGreaterThan(0);
        totalOrders.Should().BeGreaterThan(0);

        var northTeam = AccountWith(ManagerNorth).TeamName;
        var northMembers = _accounts.Where(account => account.TeamName == northTeam).Select(account => account.UserId).ToArray();
        var northContacts = Sum(contactOwners, northMembers);
        var northDeals = Sum(dealOwners, northMembers);
        var repContacts = Sum(contactOwners, [AccountWith(RepNorthFirst).UserId]);
        var repDeals = Sum(dealOwners, [AccountWith(RepNorthFirst).UserId]);
        var northOrders = Sum(orderOwners, northMembers);
        var repOrders = Sum(orderOwners, [AccountWith(RepNorthFirst).UserId]);

        // The sample is only meaningful when the team and the rep see different, non-trivial shares.
        northContacts.Should().BeInRange(1, totalContacts - 1);
        repContacts.Should().BeInRange(1, northContacts - 1);
        repOrders.Should().BeInRange(1, northOrders - 1);

        (await VisibleCountAsync(AdminEmailPrefix, ContactsPath, token)).Should().Be(totalContacts, "admin sees the organization");
        (await VisibleCountAsync(AdminEmailPrefix, DealsPath, token)).Should().Be(totalDeals);
        (await VisibleCountAsync(AdminEmailPrefix, OrdersPath, token)).Should().Be(totalOrders, "admin sees every order");
        (await VisibleCountAsync(MarketerEmailPrefix, ContactsPath, token)).Should().Be(totalContacts, "marketer reads all contacts");
        (await VisibleCountAsync(MarketerEmailPrefix, DealsPath, token)).Should().Be(totalDeals, "marketer reads all deals");
        (await VisibleCountAsync(MarketerEmailPrefix, OrdersPath, token)).Should().Be(totalOrders, "marketer reads all orders");
        (await VisibleCountAsync(ManagerNorth, ContactsPath, token)).Should().Be(northContacts, "manager sees the team's contacts");
        (await VisibleCountAsync(ManagerNorth, DealsPath, token)).Should().Be(northDeals, "manager sees the team's deals");
        (await VisibleCountAsync(ManagerNorth, OrdersPath, token)).Should().Be(northOrders, "manager sees the team's orders");
        (await VisibleCountAsync(ViewerEmailPrefix, ContactsPath, token)).Should().Be(northContacts, "viewer reads the team's contacts");
        (await VisibleCountAsync(ViewerEmailPrefix, DealsPath, token)).Should().Be(northDeals, "viewer reads the team's deals");
        (await VisibleCountAsync(ViewerEmailPrefix, OrdersPath, token)).Should().Be(northOrders, "viewer reads the team's orders");
        (await VisibleCountAsync(RepNorthFirst, ContactsPath, token)).Should().Be(repContacts, "rep sees only its own contacts");
        (await VisibleCountAsync(RepNorthFirst, DealsPath, token)).Should().Be(repDeals, "rep sees only its own deals");
        (await VisibleCountAsync(RepNorthFirst, OrdersPath, token)).Should().Be(repOrders, "rep sees only the orders of its own contacts");
        (await VisibleCountAsync(RepSouthFirst, ContactsPath, token)).Should().Be(
            Sum(contactOwners, [AccountWith(RepSouthFirst).UserId]),
            "a rep in the other team sees only its own contacts");
    }

    private DemoAccount AccountWith(string emailPrefix) =>
        _accounts.Single(account => account.Email.StartsWith(emailPrefix, StringComparison.Ordinal));

    private static int Sum(IReadOnlyDictionary<Guid, int> counts, IEnumerable<Guid> ids) =>
        ids.Sum(id => counts.GetValueOrDefault(id));

    /// <summary>Signs in the account whose address starts with the prefix and counts every page of the list it can see.</summary>
    private async Task<int> VisibleCountAsync(string emailPrefix, string path, CancellationToken cancellationToken)
    {
        var account = AccountWith(emailPrefix);
        using var signIn = AuthTestClient.CreateClient(_factory, CultureCodes.English);
        using var response = await AuthTestClient.SignInAsync(signIn, account.Email, account.Password!, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"{account.RoleCode} signs in");
        var tokens = await AuthTestClient.ReadTokensAsync(response, cancellationToken);

        using var client = CrmApi.Client(_factory, tokens.AccessToken);
        var count = 0;
        string? after = null;
        do
        {
            var query = $"{path}?limit={PageSize}" + (after is null ? string.Empty : "&after=" + Uri.EscapeDataString(after));
            using var page = await client.GetAsync(query, cancellationToken);
            page.StatusCode.Should().Be(HttpStatusCode.OK, $"{account.RoleCode} lists {path}");
            using var json = JsonDocument.Parse(await page.Content.ReadAsStringAsync(cancellationToken));
            count += json.RootElement.GetProperty(ItemsProperty).GetArrayLength();
            after = json.RootElement.TryGetProperty(NextCursorProperty, out var cursor) && cursor.ValueKind == JsonValueKind.String
                ? cursor.GetString()
                : null;
        }
        while (after is not null);

        return count;
    }

    /// <summary>Number of demo orders per owner of their contact: orders have no owner of their own.</summary>
    private async Task<Dictionary<Guid, int>> OrderOwnerCountsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _factory.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT c.owner_user_id, COUNT(*) FROM crm.orders o JOIN crm.contacts c ON c.id = o.contact_id "
            + "WHERE o.organization_id = @org AND o.source = @source GROUP BY c.owner_user_id",
            connection);
        command.Parameters.AddWithValue("org", _organizationId);
        command.Parameters.AddWithValue("source", OrderSources.DemoSeed);
        var counts = new Dictionary<Guid, int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            counts[reader.GetGuid(0)] = Convert.ToInt32(reader.GetInt64(1));
        }

        return counts;
    }

    /// <summary>Number of demo rows per owner, read straight from the table: the expected share of each account.</summary>
    private async Task<Dictionary<Guid, int>> OwnerCountsAsync(string table, string source, CancellationToken cancellationToken)
    {
        await using var connection = await _factory.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            $"SELECT owner_user_id, COUNT(*) FROM crm.{table} WHERE organization_id = @org AND source = @source GROUP BY owner_user_id",
            connection);
        command.Parameters.AddWithValue("org", _organizationId);
        command.Parameters.AddWithValue("source", source);
        var counts = new Dictionary<Guid, int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            counts[reader.GetGuid(0)] = Convert.ToInt32(reader.GetInt64(1));
        }

        return counts;
    }
}
