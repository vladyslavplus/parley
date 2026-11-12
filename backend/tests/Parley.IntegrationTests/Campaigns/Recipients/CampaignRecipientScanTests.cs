using System.Net;
using AwesomeAssertions;
using Npgsql;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Shared;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Paging;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Recipients;

/// <summary>
/// A recipient filter that matches nothing scans at most <see cref="CampaignsLimits.RecipientScanCap"/> rows in one request.
/// The cursor it returns is the scan position, so the client continues from the last row this request scanned.
/// </summary>
public sealed class CampaignRecipientScanTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const int PageSize = 10;

    private const string ItemsProperty = "items";

    private const string CursorProperty = "nextCursor";

    private const string FreezeSql = "UPDATE campaigns.campaigns SET audience_frozen_at = now() WHERE id = @campaign";

    private const string InsertRecipientsSql = """
        INSERT INTO campaigns.recipients
            (id, organization_id, created_at, updated_at, campaign_id, contact_id, arm, assignment_bucket, is_targeted, locale)
        SELECT gen_random_uuid(), @org, now(), now(), @campaign, gen_random_uuid(), @arm, 0, true, 'en'
        FROM generate_series(1, @count)
        """;

    private const string RecipientIdAtSql =
        "SELECT id FROM campaigns.recipients WHERE campaign_id = @campaign ORDER BY id OFFSET @skip LIMIT 1";

    private const string ParameterCampaign = "campaign";

    private const string ParameterOrganization = "org";

    private const string ParameterArm = "arm";

    private const string ParameterCount = "count";

    private const string ParameterSkip = "skip";

    private IdentityApiFactory _factory = null!;

    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _connectionString = postgres.ConnectionString;
        _factory = new IdentityApiFactory(postgres, mailpit);
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task FilterThatMatchesNothing_ScansAtMostTheCap_AndCursorIsTheScanPosition()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);

        // One row more than the cap, so the audience is not exhausted within the first request. The campaign and its rows
        // are written by SQL: a real freeze of that many contacts would be slow, and the list reads only the frozen flag.
        await ExecuteAsync(FreezeSql, (ParameterCampaign, campaignId));
        await ExecuteAsync(
            InsertRecipientsSql,
            (ParameterOrganization, owner.OrganizationId),
            (ParameterCampaign, campaignId),
            (ParameterArm, HoldoutArms.Treatment),
            (ParameterCount, CampaignsLimits.RecipientScanCap + 1));

        var response = await admin.GetAsync(
            $"{IdentityApi.Route(CampaignsRoutes.CampaignRecipients, campaignId)}?sendStatus={SendStatuses.Failed}&limit={PageSize}",
            token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, token);

        page.GetProperty(ItemsProperty).GetArrayLength().Should().Be(0, "no recipient has a failed send");
        var cursor = page.GetProperty(CursorProperty).GetString();
        cursor.Should().NotBeNull("the scan stopped at the cap while the audience had more rows");

        KeysetCursor.TryDecode(cursor!, out var decoded).Should().BeTrue();
        decoded.Should().NotBeNull();
        decoded!.Id.Should().Be(
            await ReadRecipientIdAtAsync(campaignId, CampaignsLimits.RecipientScanCap - 1, token),
            "the cursor names the last row of the cap, so the next request starts after it");
    }

    private async Task<Guid> ReadRecipientIdAtAsync(Guid campaignId, int skip, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(RecipientIdAtSql, connection);
        command.Parameters.AddWithValue(ParameterCampaign, campaignId);
        command.Parameters.AddWithValue(ParameterSkip, skip);
        return (Guid)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    private async Task ExecuteAsync(string sql, params (string Name, object Value)[] parameters)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
