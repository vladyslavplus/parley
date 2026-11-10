using System.Net;
using AwesomeAssertions;
using Npgsql;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Domain.Recipients;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Recipients;

/// <summary>
/// A recipient's name, conversion and revenue follow the caller's contacts scope. A team-scoped sales manager sees the names
/// of team contacts only; a contact outside the team is listed with its name hidden and no conversion shown.
/// </summary>
public sealed class CampaignRecipientNameScopeTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int HoldoutShareBps = 1000;

    private const string ItemsProperty = "items";

    private const string ContactIdProperty = "contactId";

    private const string ContactNameProperty = "contactName";

    private const string NameVisibleProperty = "nameVisible";

    private const string ConversionProperty = "conversion";

    private const string NotVisibleConversion = "not_visible";

    private const string FreezeSql = "UPDATE campaigns.campaigns SET audience_frozen_at = now() WHERE id = @campaign";

    private const string InsertRecipientSql = """
        INSERT INTO campaigns.recipients
            (id, organization_id, created_at, updated_at, campaign_id, contact_id, arm, assignment_bucket, is_targeted, locale)
        VALUES (gen_random_uuid(), @org, now(), now(), @campaign, @contact, @arm, 0, true, 'en')
        """;

    private const string ParameterCampaign = "campaign";

    private const string ParameterOrganization = "org";

    private const string ParameterContact = "contact";

    private const string ParameterArm = "arm";

    private string _connectionString = null!;

    private IdentityApiFactory _factory = null!;

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
    public async Task TeamScopedManager_SeesNamesOnlyInsideTheTeam()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repInTeam = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repOutside = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var manager = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesManagerRoleId, token);
        await CrmApi.CreateTeamAsync(_factory, admin, owner.OrganizationId, [repInTeam.UserId, manager.UserId], token);
        var insideId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repInTeam.UserId), token));
        var outsideId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repOutside.UserId), token));

        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await ExecuteAsync(InsertRecipientSql, (ParameterOrganization, owner.OrganizationId), (ParameterCampaign, campaignId), (ParameterContact, insideId), (ParameterArm, HoldoutArms.Treatment));
        await ExecuteAsync(InsertRecipientSql, (ParameterOrganization, owner.OrganizationId), (ParameterCampaign, campaignId), (ParameterContact, outsideId), (ParameterArm, HoldoutArms.Treatment));
        await ExecuteAsync(FreezeSql, (ParameterCampaign, campaignId));

        var ownerView = await ReadRecipientsAsync(admin, campaignId, token);
        ownerView[insideId].NameVisible.Should().BeTrue();
        ownerView[outsideId].NameVisible.Should().BeTrue("an organization-wide reader sees every name");

        var managerView = await ReadRecipientsAsync(CrmApi.Client(_factory, manager.AccessToken), campaignId, token);
        managerView[insideId].NameVisible.Should().BeTrue();
        managerView[insideId].ContactName.Should().NotBeNullOrWhiteSpace();
        managerView[outsideId].NameVisible.Should().BeFalse();
        managerView[outsideId].ContactName.Should().BeNull("the name of a contact outside the team is never sent");
        managerView[outsideId].Conversion.Should().Be(NotVisibleConversion);
    }

    private static async Task<Dictionary<Guid, RecipientVisibility>> ReadRecipientsAsync(HttpClient client, Guid campaignId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{IdentityApi.Route(CampaignsRoutes.CampaignRecipients, campaignId)}?limit=50", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray().ToDictionary(
            item => item.GetProperty(ContactIdProperty).GetGuid(),
            item => new RecipientVisibility(
                item.GetProperty(NameVisibleProperty).GetBoolean(),
                item.GetProperty(ContactNameProperty).GetString(),
                item.GetProperty(ConversionProperty).GetString()!));
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

    private sealed record RecipientVisibility(bool NameVisible, string? ContactName, string Conversion);
}
