using System.Globalization;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.DemoSeed;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Crm.DemoSeed;

/// <summary>
/// The generated demo workspace: accounts, contacts, orders, companies, deals, tasks, notes, tags and the timeline, and a
/// re-run that changes nothing. The profile is small, so the suite stays fast; the production volumes are defaults.
/// </summary>
public sealed class DemoSeedTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string SharedPassword = "test1234";

    private static readonly DateTimeOffset Reference = new(2026, 11, 12, 10, 0, 0, TimeSpan.Zero);

    private static readonly CrmDemoProfile Profile = new()
    {
        Contacts = 120,
        OrderMonths = 12,
        CompanySharePercent = 30,
        DealSharePercent = 60,
        TaskSharePercent = 40,
        NoteSharePercent = 25,
        TagSharePercent = 45,
        NoOrdersPercent = 5,
        CancelledOrderPercent = 2,
    };

    private readonly Guid _organizationId = Guid.CreateVersion7();

    public async ValueTask InitializeAsync() => await postgres.ResetDataAsync(TestContext.Current.CancellationToken);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FirstSeedCreatesTheWorkspace_AndAReferenceInstantWithoutFutureOrders()
    {
        var (identity, report) = await SeedAsync();

        identity.OrganizationCreated.Should().BeTrue();
        identity.UsersCreated.Should().Be(DemoAccountCatalogSize());
        report.PipelineCreated.Should().BeTrue();
        report.ContactsInserted.Should().Be(Profile.Contacts);
        report.OrdersInserted.Should().BeGreaterThan(Profile.Contacts);
        report.OrdersWithoutContact.Should().Be(0);
        report.LatestOrderAt.Should().NotBeNull();
        report.LatestOrderAt!.Value.Should().BeBefore(Reference);
        report.LatestOrderAt.Value.Date.Should().Be(Reference.UtcDateTime.Date.AddDays(-1), "the order history ends yesterday");

        var owners = identity.Accounts.Where(account => account.RoleCode == "sales_rep").Select(account => account.UserId).ToArray();
        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org AND source = 'demo_seed' AND NOT (owner_user_id = ANY(@owners))", owners))
            .Should().Be(0, "every demo contact belongs to a sales rep");
        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org AND source = 'demo_seed' AND (city IS NULL OR email NOT LIKE '%@demo.parley.test')", owners))
            .Should().Be(0, "every generated contact has a city and a safe demo address");
        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org AND source = 'demo_seed' AND company_id IS NOT NULL", owners))
            .Should().BeGreaterThan(0);
        (await CountAsync("SELECT COUNT(*) FROM crm.deals d JOIN crm.pipeline_stages s ON s.id = d.stage_id WHERE d.organization_id = @org AND d.source = 'demo_seed' AND d.status <> CASE s.kind WHEN 'won' THEN 'won' WHEN 'lost' THEN 'lost' ELSE 'open' END", owners))
            .Should().Be(0, "a deal's status follows its stage kind");
        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org AND source = 'demo_seed' AND last_order_at > @reference", owners, ("reference", Reference.UtcDateTime)))
            .Should().Be(0, "no order lies after the reference instant");
        (await CountAsync("SELECT COUNT(*) FROM crm.activities WHERE organization_id = @org AND type = 'order_recorded'", owners))
            .Should().Be(report.OrdersInserted, "every order has a timeline entry");
        (await CountAsync("SELECT COUNT(DISTINCT tag_id) FROM crm.contact_tags WHERE organization_id = @org", owners))
            .Should().BeGreaterThan(1, "tag links are generated");
    }

    [Fact]
    public async Task RerunningTheSameProfileChangesNothing()
    {
        await SeedAsync();
        var before = await SnapshotAsync();

        var (identityRerun, rerun) = await SeedAsync();

        identityRerun.OrganizationCreated.Should().BeFalse();
        identityRerun.UsersCreated.Should().Be(0);
        identityRerun.MembershipsCreated.Should().Be(0);
        rerun.PipelineCreated.Should().BeFalse();
        rerun.ContactsInserted.Should().Be(0);
        rerun.OrdersInserted.Should().Be(0);
        rerun.CompaniesInserted.Should().Be(0);
        rerun.DealsInserted.Should().Be(0);
        rerun.TasksInserted.Should().Be(0);
        rerun.NotesInserted.Should().Be(0);
        rerun.ActivitiesInserted.Should().Be(0);
        (await SnapshotAsync()).Should().Equal(before);
    }

    private async Task<(IdentityDemoSeedResult Identity, CrmDemoImportReport Crm)> SeedAsync()
    {
        using var scope = postgres.Factory.Services.CreateScope();
        var token = TestContext.Current.CancellationToken;
        var profile = new DemoOrganizationProfile(
            _organizationId,
            "Parley Demo",
            "parley-demo-" + _organizationId.ToString("N")[..8],
            "Europe/Kyiv",
            CultureCodes.Ukrainian,
            CrmDemoContract.DemoCurrency);
        var identity = await scope.ServiceProvider.GetRequiredService<IIdentityDemoSeeder>().EnsureAsync(profile, SharedPassword, token);
        var owners = identity.Accounts.Where(account => account.RoleCode == "sales_rep").Select(account => account.UserId).ToArray();
        var crm = await scope.ServiceProvider.GetRequiredService<ICrmDemoSeeder>().SeedAsync(
            new CrmDemoImportRequest(_organizationId, CrmDemoContract.DemoCurrency, CultureCodes.Ukrainian, owners, 42, Reference, Profile),
            token);
        return (identity, crm);
    }

    private static int DemoAccountCatalogSize() => 12;

    private async Task<IReadOnlyList<string>> SnapshotAsync()
    {
        var lines = new List<string>();
        foreach (var table in new[] { "contacts", "companies", "orders", "deals", "deal_stage_history", "tasks", "notes", "activities", "contact_tags" })
        {
            lines.Add($"{table}={await CountAsync($"SELECT COUNT(*) FROM crm.{table} WHERE organization_id = @org", [], [])}");
        }

        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT id::text || '|' || status || '|' || amount::text FROM crm.deals WHERE organization_id = @org ORDER BY id", connection);
        command.Parameters.AddWithValue("org", _organizationId);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            lines.Add(reader.GetString(0));
        }

        return lines;
    }

    private async Task<long> CountAsync(string sql, Guid[] owners, params (string Name, object Value)[] extra)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("org", _organizationId);
        if (sql.Contains("@owners", StringComparison.Ordinal))
        {
            command.Parameters.AddWithValue("owners", owners);
        }

        foreach (var (name, value) in extra)
        {
            command.Parameters.AddWithValue(name, value);
        }

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), CultureInfo.InvariantCulture);
    }

    private async Task<NpgsqlConnection> OpenAsync() =>
        await postgres.Factory.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync(TestContext.Current.CancellationToken);
}
