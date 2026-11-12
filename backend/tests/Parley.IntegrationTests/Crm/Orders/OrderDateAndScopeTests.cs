using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Crm.Orders;

/// <summary>
/// Orders follow the contacts the caller may see under the orders read scope, and the date filter takes instants: the
/// lower bound is inclusive and the upper bound exclusive, so a day in the caller's time zone maps to a precise range.
/// </summary>
public sealed class OrderDateAndScopeTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int CustomerCount = 40;

    private const string ItemsProperty = "items";

    private const string ErrorCodeProperty = "code";

    private const string ValidationFailedCode = "common.validation.failed";

    private const string InstantFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private const string DemoSource = "demo_seed";

    private IdentityApiFactory _factory = null!;

    private OrganizationOwner _owner = null!;

    private CrmApi.Member _rep = null!;

    private Guid _otherRepUserId;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);

        var token = TestContext.Current.CancellationToken;
        _owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        _rep = await CrmApi.AddMemberAsync(_factory, _owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var other = await CrmApi.AddMemberAsync(_factory, _owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        _otherRepUserId = other.UserId;

        using var scope = _factory.Services.CreateScope();
        var profile = new CrmDemoProfile
        {
            Contacts = CustomerCount,
            OrderMonths = 12,
            DealSharePercent = 0,
            TaskSharePercent = 0,
            NoteSharePercent = 0,
            TagSharePercent = 0,
            NoOrdersPercent = 0,
        };
        await scope.ServiceProvider.GetRequiredService<ICrmDemoSeeder>().SeedAsync(
            new CrmDemoImportRequest(
                _owner.OrganizationId,
                CrmDemoContract.DemoCurrency,
                CultureCodes.Ukrainian,
                [_rep.UserId, _otherRepUserId],
                11,
                _factory.Clock.GetUtcNow(),
                profile),
            token);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task A_rep_lists_and_reads_only_orders_of_its_own_contacts()
    {
        var token = TestContext.Current.CancellationToken;
        var ownOrders = await CountOrdersOwnedByAsync(_rep.UserId, token);
        var otherOrders = await CountOrdersOwnedByAsync(_otherRepUserId, token);
        ownOrders.Should().BeGreaterThan(0);
        otherOrders.Should().BeGreaterThan(0);

        using var repClient = CrmApi.Client(_factory, _rep.AccessToken);
        var listed = await ListAllAsync(repClient, "orders?limit=100", token);
        listed.Should().HaveCount(ownOrders, "the rep sees only the orders of its contacts");

        var foreignOrderId = await OrderIdOwnedByAsync(_otherRepUserId, token);
        using var foreign = await repClient.GetAsync($"orders/{foreignOrderId:D}", token);
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound, "an order outside the scope does not exist for the caller");

        var ownOrderId = await OrderIdOwnedByAsync(_rep.UserId, token);
        using var own = await repClient.GetAsync($"orders/{ownOrderId:D}", token);
        own.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_owner_sees_every_order()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CrmApi.Client(_factory, _owner.AccessToken);

        var listed = await ListAllAsync(client, "orders?limit=100", token);

        var total = await CountOrdersOwnedByAsync(_rep.UserId, token) + await CountOrdersOwnedByAsync(_otherRepUserId, token);
        listed.Should().HaveCount(total);
    }

    [Fact]
    public async Task The_lower_bound_is_inclusive_and_the_upper_bound_exclusive()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CrmApi.Client(_factory, _owner.AccessToken);
        var instant = await OrderInstantAsync(offset: 10, token);

        var fromInstant = await ListAllAsync(client, $"orders?orderedFrom={Instant(instant)}&orderedTo={Instant(instant.AddHours(1))}", token);
        fromInstant.Should().ContainSingle("an order exactly at the lower bound is included");

        var toInstant = await ListAllAsync(client, $"orders?orderedFrom={Instant(instant.AddHours(-1))}&orderedTo={Instant(instant)}", token);
        toInstant.Should().NotContain(fromInstant.Single(), "an order exactly at the upper bound is excluded");
    }

    [Fact]
    public async Task A_range_that_ends_before_it_starts_is_a_validation_error()
    {
        var token = TestContext.Current.CancellationToken;
        using var client = CrmApi.Client(_factory, _owner.AccessToken);
        var instant = await OrderInstantAsync(offset: 10, token);

        using var response = await client.GetAsync(
            $"orders?orderedFrom={Instant(instant)}&orderedTo={Instant(instant.AddHours(-1))}",
            token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        problem.RootElement.GetProperty(ErrorCodeProperty).GetString().Should().Be(ValidationFailedCode);
    }

    private static string Instant(DateTime instant) => instant.ToString(InstantFormat, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Reads every page of an order list and returns the identifiers in order.</summary>
    private static async Task<List<string>> ListAllAsync(HttpClient client, string path, CancellationToken cancellationToken)
    {
        var ids = new List<string>();
        string? after = null;
        do
        {
            var separator = path.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var url = after is null ? path : $"{path}{separator}after={Uri.EscapeDataString(after)}";
            using var response = await client.GetAsync(url, cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            foreach (var item in json.RootElement.GetProperty(ItemsProperty).EnumerateArray())
            {
                ids.Add(item.GetProperty("id").GetString()!);
            }

            after = json.RootElement.TryGetProperty("nextCursor", out var cursor) && cursor.ValueKind == JsonValueKind.String
                ? cursor.GetString()
                : null;
        }
        while (after is not null);

        return ids;
    }

    private async Task<DateTime> OrderInstantAsync(int offset, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT ordered_at FROM crm.orders WHERE organization_id = @org AND source = @source "
            + "GROUP BY ordered_at HAVING COUNT(*) = 1 ORDER BY ordered_at OFFSET @offset LIMIT 1",
            connection);
        command.Parameters.AddWithValue("org", _owner.OrganizationId);
        command.Parameters.AddWithValue("source", DemoSource);
        command.Parameters.AddWithValue("offset", offset);
        return (DateTime)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task<int> CountOrdersOwnedByAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM crm.orders o JOIN crm.contacts c ON c.id = o.contact_id "
            + "WHERE o.organization_id = @org AND o.source = @source AND c.owner_user_id = @owner",
            connection);
        command.Parameters.AddWithValue("org", _owner.OrganizationId);
        command.Parameters.AddWithValue("source", DemoSource);
        command.Parameters.AddWithValue("owner", ownerUserId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<Guid> OrderIdOwnedByAsync(Guid ownerUserId, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT o.id FROM crm.orders o JOIN crm.contacts c ON c.id = o.contact_id "
            + "WHERE o.organization_id = @org AND o.source = @source AND c.owner_user_id = @owner LIMIT 1",
            connection);
        command.Parameters.AddWithValue("org", _owner.OrganizationId);
        command.Parameters.AddWithValue("source", DemoSource);
        command.Parameters.AddWithValue("owner", ownerUserId);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken) =>
        await _factory.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync(cancellationToken);
}
