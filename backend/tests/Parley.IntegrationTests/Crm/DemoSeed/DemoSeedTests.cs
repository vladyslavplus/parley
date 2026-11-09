using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.DemoSeed;
using Parley.IntegrationTests.Crm.DemoSeed.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Crm.DemoSeed;

/// <summary>
/// The demo workspace seed: accounts and teams, the export with dates moved to yesterday, the generated companies,
/// deals, tasks, notes and timeline, and a re-run that changes nothing.
/// </summary>
public sealed class DemoSeedTests(PostgresFixture postgres) : IAsyncLifetime
{
    private static readonly DateTimeOffset LatestSourceOrderAfterShift = new(2026, 11, 7, 12, 50, 0, TimeSpan.Zero);

    private readonly Guid _organizationId = Guid.CreateVersion7();

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "parley-demo-seed-" + Guid.CreateVersion7().ToString("N"));

    private readonly string _workspaceDirectory = Path.Combine(Path.GetTempPath(), "parley-demo-workspace-" + Guid.CreateVersion7().ToString("N"));

    /// <summary>Customers of the workspace export: enough recent buyers for deals, stage changes and closures.</summary>
    private const int WorkspaceCustomerCount = 60;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        DemoExportFiles.WriteSample(_directory);
    }

    public ValueTask DisposeAsync()
    {
        foreach (var directory in new[] { _directory, _workspaceDirectory })
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task FirstImportCreatesTheWorkspaceAndShiftsDatesToYesterdayKeepingIntervals()
    {
        var (identity, report) = await SeedAsync();

        identity.OrganizationCreated.Should().BeTrue();
        identity.UsersCreated.Should().Be(11);
        identity.Accounts.All(account => account.Password is not null).Should().BeTrue();
        report.PipelineCreated.Should().BeTrue();
        report.ContactsInserted.Should().Be(3);
        report.OrdersInserted.Should().Be(5);
        report.OrdersWithoutContact.Should().Be(0);
        report.LatestOrderAt.Should().Be(LatestSourceOrderAfterShift);

        var orders = await ReadDemoOrderTimesAsync();
        (orders["1002"] - orders["1001"]).Should().Be(new DateTime(2011, 12, 9, 12, 50, 0) - new DateTime(2011, 6, 1, 10, 0, 0));
        orders.Values.Max().Should().Be(LatestSourceOrderAfterShift.UtcDateTime);
    }

    [Fact]
    public async Task EveryGeneratedRowFollowsTheWorkspaceRules()
    {
        var (identity, report) = await SeedAsync();
        var owners = identity.Accounts.Where(account => account.RoleCode == "sales_rep").Select(account => account.UserId).ToArray();

        (await CountAsync("SELECT COUNT(*) FROM crm.contacts WHERE organization_id = @org AND source = 'demo_seed' AND NOT (owner_user_id = ANY(@owners))", owners))
            .Should().Be(0, "every demo contact belongs to a sales rep");
        (await CountAsync("SELECT COUNT(*) FROM crm.deals d JOIN crm.pipeline_stages s ON s.id = d.stage_id WHERE d.organization_id = @org AND d.source = 'demo_seed' AND d.status <> CASE s.kind WHEN 'won' THEN 'won' WHEN 'lost' THEN 'lost' ELSE 'open' END", owners))
            .Should().Be(0, "a deal's status follows its stage kind");
        (await CountAsync("SELECT COUNT(*) FROM crm.deals WHERE organization_id = @org AND source = 'demo_seed' AND (status = 'open') = (closed_at IS NOT NULL)", owners))
            .Should().Be(0, "only open deals lack a closing instant");
        (await CountAsync("SELECT COUNT(*) FROM crm.deals d WHERE d.organization_id = @org AND d.source = 'demo_seed' AND d.amount <> COALESCE((SELECT o.total_amount FROM crm.orders o WHERE o.contact_id = d.contact_id AND NOT o.is_cancellation ORDER BY o.ordered_at DESC LIMIT 1), 0)", owners))
            .Should().Be(0, "a deal amount is the latest purchase total");
        (await CountAsync("SELECT COUNT(*) FROM crm.deals d WHERE d.organization_id = @org AND d.source = 'demo_seed' AND NOT EXISTS (SELECT 1 FROM crm.deal_stage_history h WHERE h.deal_id = d.id)", owners))
            .Should().Be(0, "every demo deal has a stage history");
        (await CountAsync("SELECT COUNT(*) FROM crm.activities WHERE organization_id = @org AND type = 'order_recorded'", owners))
            .Should().Be(report.OrdersInserted, "every order has a timeline entry");
        (await CountAsync("SELECT COUNT(*) FROM crm.activities WHERE organization_id = @org AND type = 'contact_created'", owners))
            .Should().Be(report.ContactsInserted);
        (await CountAsync("SELECT COUNT(*) FROM crm.companies WHERE organization_id = @org", owners))
            .Should().Be(await CountAsync("SELECT COUNT(DISTINCT company_id) FROM crm.contacts WHERE organization_id = @org AND company_id IS NOT NULL", owners));
    }

    [Fact]
    public async Task OrderEntriesCarryTheFieldsTheTimelineRendersAndNoActor()
    {
        var (_, report) = await SeedAsync();

        report.OrdersInserted.Should().BeGreaterThan(0);
        (await CountAsync(
            """
            SELECT COUNT(*) FROM crm.activities
            WHERE organization_id = @org AND type = 'order_recorded' AND (
                actor_user_id IS NOT NULL
                OR (SELECT COUNT(*) FROM jsonb_object_keys(payload)) <> 4
                OR NOT (payload ?& ARRAY['invoiceNo', 'totalAmount', 'currency', 'isCancellation'])
                OR jsonb_typeof(payload->'totalAmount') <> 'number'
                OR jsonb_typeof(payload->'isCancellation') <> 'boolean'
                OR payload->>'currency' <> 'GBP')
            """,
            []))
            .Should().Be(0, "every order entry has the four payload fields, an amount as a number, and no actor");
    }

    [Fact]
    public async Task StageAndClosureEntriesCarryStageNamesStatusAndTheDealOwnerAsActor()
    {
        var workspace = DemoExportFiles.WriteWorkspace(_workspaceDirectory, WorkspaceCustomerCount);
        var (_, report) = await SeedAsync(workspace);

        report.DealStageHistoryInserted.Should().BeGreaterThan(0);
        (await CountAsync("SELECT COUNT(*) FROM crm.activities WHERE organization_id = @org AND type = 'deal_closed'", []))
            .Should().BeGreaterThan(0, "the sample export closes at least one deal");

        (await CountAsync(
            """
            SELECT COUNT(*) FROM crm.activities a
            JOIN crm.deals d ON d.id = a.deal_id
            JOIN crm.pipeline_stages fs ON fs.id = (a.payload->>'fromStageId')::uuid
            JOIN crm.pipeline_stages ts ON ts.id = (a.payload->>'toStageId')::uuid
            WHERE a.organization_id = @org AND a.type = 'deal_stage_changed' AND (
                a.actor_user_id IS DISTINCT FROM d.owner_user_id
                OR (SELECT COUNT(*) FROM jsonb_object_keys(a.payload)) <> 5
                OR NOT (a.payload ?& ARRAY['fromStageId', 'fromStageName', 'toStageId', 'toStageName', 'status'])
                OR a.payload->>'fromStageName' <> fs.name
                OR a.payload->>'toStageName' <> ts.name
                OR a.payload->>'status' <> ts.kind)
            """,
            []))
            .Should().Be(0, "a stage change names both stages in the organization locale and carries the deal owner as actor");

        (await CountAsync(
            """
            SELECT COUNT(*) FROM crm.activities a
            JOIN crm.deals d ON d.id = a.deal_id
            JOIN crm.pipeline_stages st ON st.id = d.stage_id
            WHERE a.organization_id = @org AND a.type = 'deal_closed' AND (
                a.actor_user_id IS DISTINCT FROM d.owner_user_id
                OR (SELECT COUNT(*) FROM jsonb_object_keys(a.payload)) <> 2
                OR NOT (a.payload ?& ARRAY['stageName', 'status'])
                OR a.payload->>'stageName' <> st.name
                OR a.payload->>'status' <> st.kind)
            """,
            []))
            .Should().Be(0, "a closure names the closing stage and carries the deal owner as actor");
    }

    [Fact]
    public async Task RerunningTheSameExportChangesNothing()
    {
        await SeedAsync();
        var before = await SnapshotAsync();

        var (identityRerun, rerun) = await SeedAsync();

        identityRerun.OrganizationCreated.Should().BeFalse();
        identityRerun.UsersCreated.Should().Be(0);
        identityRerun.MembershipsCreated.Should().Be(0);
        identityRerun.Accounts.All(account => account.Password is null).Should().BeTrue();
        rerun.PipelineCreated.Should().BeFalse();
        rerun.ContactsInserted.Should().Be(0);
        rerun.ContactsSkipped.Should().Be(3);
        rerun.OrdersInserted.Should().Be(0);
        rerun.OrdersSkipped.Should().Be(5);
        rerun.ContactsLastOrderUpdated.Should().Be(0);
        rerun.CompaniesInserted.Should().Be(0);
        rerun.DealsInserted.Should().Be(0);
        rerun.DealStageHistoryInserted.Should().Be(0);
        rerun.TasksInserted.Should().Be(0);
        rerun.NotesInserted.Should().Be(0);
        rerun.ActivitiesInserted.Should().Be(0);
        (await SnapshotAsync()).Should().Equal(before);
    }

    private async Task<(IdentityDemoSeedResult Identity, CrmDemoImportReport Crm)> SeedAsync(string? directory = null)
    {
        using var scope = postgres.Factory.Services.CreateScope();
        var token = TestContext.Current.CancellationToken;
        var profile = new DemoOrganizationProfile(_organizationId, "Parley Demo", "parley-demo-" + _organizationId.ToString("N")[..8], "Europe/Kyiv", CultureCodes.Ukrainian, CrmDemoContract.ExportCurrency);
        var identity = await scope.ServiceProvider.GetRequiredService<IIdentityDemoDataImporter>().ImportAsync(profile, resetPasswords: false, token);
        var owners = identity.Accounts.Where(account => account.RoleCode == "sales_rep").Select(account => account.UserId).ToArray();
        var crm = await scope.ServiceProvider.GetRequiredService<ICrmDemoDataImporter>().ImportAsync(
            new CrmDemoImportRequest(_organizationId, directory ?? _directory, CrmDemoContract.ExportCurrency, CultureCodes.Ukrainian, owners),
            token);
        return (identity, crm);
    }

    private async Task<Dictionary<string, DateTime>> ReadDemoOrderTimesAsync()
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT external_ref, ordered_at FROM crm.orders WHERE organization_id = @org AND source = 'demo_seed'", connection);
        command.Parameters.AddWithValue("org", _organizationId);
        var result = new Dictionary<string, DateTime>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        while (await reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            result[reader.GetString(0)] = reader.GetDateTime(1);
        }

        return result;
    }

    /// <summary>Row counts of the workspace tables plus the deal rows, in a stable order.</summary>
    private async Task<IReadOnlyList<string>> SnapshotAsync()
    {
        var lines = new List<string>();
        foreach (var table in new[] { "contacts", "companies", "orders", "deals", "deal_stage_history", "tasks", "notes", "activities" })
        {
            lines.Add($"{table}={await CountAsync($"SELECT COUNT(*) FROM crm.{table} WHERE organization_id = @org", [])}");
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

    private async Task<long> CountAsync(string sql, Guid[] owners)
    {
        await using var connection = await OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("org", _organizationId);
        if (sql.Contains("@owners", StringComparison.Ordinal))
        {
            command.Parameters.AddWithValue("owners", owners);
        }

        return Convert.ToInt64(await command.ExecuteScalarAsync(TestContext.Current.CancellationToken), System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<NpgsqlConnection> OpenAsync() =>
        await postgres.Factory.Services.GetRequiredService<NpgsqlDataSource>().OpenConnectionAsync(TestContext.Current.CancellationToken);
}
