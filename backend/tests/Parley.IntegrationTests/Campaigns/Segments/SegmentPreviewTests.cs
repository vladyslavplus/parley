using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Npgsql;
using Parley.Api;
using Parley.Api.Controllers.Campaigns;
using Parley.Api.Controllers.Crm;
using Parley.Campaigns.Contracts.Errors;
using Parley.Crm.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Segments;

/// <summary>
/// The segment preview counts the eligible contacts a definition selects with the shared audience rule, and reports
/// the RFM filter with its own code.
/// </summary>
public sealed class SegmentPreviewTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string CountProperty = "count";

    private const string SampleProperty = "sample";

    private const string IdProperty = "id";

    private const short TagColorIndex = 1;

    private const int RecentOrderDays = 10;

    private const int OldOrderDays = 100;

    private const int WindowDays = 30;

    private static readonly string UpdateLastOrderSql =
        $"UPDATE \"{CrmSchemaNames.Schema}\".\"{CrmSchemaNames.ContactsTable}\" SET last_order_at = @at WHERE id = @id";

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
    public async Task Preview_CountsEligibleContacts_AndRejectsRfm()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await CrmApi.CreateContactAsync(admin, new ContactBody("Anna", "Lind", "anna@preview.example.test", null, null, "DE", null, null, null, null), token);
        await CrmApi.CreateContactAsync(admin, new ContactBody("Bo", "Berg", "bo@preview.example.test", null, null, "DE", null, null, null, null), token);
        await CrmApi.CreateContactAsync(admin, new ContactBody("Chloe", "Dubois", "chloe@preview.example.test", null, null, "FR", null, null, null, null), token);
        await CrmApi.CreateContactAsync(admin, new ContactBody("No", "Email", null, null, null, "DE", null, null, null, null), token);

        var german = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, new { countryCodes = new[] { "de" } }, token);
        german.StatusCode.Should().Be(HttpStatusCode.OK);
        var germanView = await AuthTestClient.ReadJsonAsync(german, token);
        germanView.GetProperty(CountProperty).GetInt32().Should().Be(2, "the contact without an address is not eligible");
        germanView.GetProperty(SampleProperty).GetArrayLength().Should().Be(2);

        var everyone = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, new { }, token);
        (await AuthTestClient.ReadJsonAsync(everyone, token)).GetProperty(CountProperty).GetInt32().Should().Be(3);

        var rfm = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, new { rfmSegments = new[] { "champions" } }, token);
        rfm.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await AuthTestClient.ReadJsonAsync(rfm, token);
        problem.GetProperty(ApiConstants.ProblemCode).GetString().Should().Be(CampaignsErrorCodes.SegmentRfmNotAvailable);
    }

    [Fact]
    public async Task Preview_TagFilter_CountsOnlyContactsCarryingEveryListedTag()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var vip = await CreateTagAsync(admin, "vip", token);
        var newsletter = await CreateTagAsync(admin, "newsletter", token);
        var anna = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, new ContactBody("Anna", "Lind", "anna@tags.example.test", null, null, "DE", null, null, null, null), token));
        var bo = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, new ContactBody("Bo", "Berg", "bo@tags.example.test", null, null, "DE", null, null, null, null), token));
        await CrmApi.CreateContactAsync(admin, new ContactBody("Chloe", "Dubois", "chloe@tags.example.test", null, null, "FR", null, null, null, null), token);
        await AttachTagsAsync(admin, anna, [vip, newsletter], token);
        await AttachTagsAsync(admin, bo, [vip], token);

        var withVip = await PreviewAsync(admin, new { tagIds = new[] { vip } }, token);
        withVip.GetProperty(CountProperty).GetInt32().Should().Be(2, "Anna and Bo carry the vip tag");

        var withBoth = await PreviewAsync(admin, new { tagIds = new[] { vip, newsletter } }, token);
        withBoth.GetProperty(CountProperty).GetInt32().Should().Be(1, "only Anna carries both tags");
        withBoth.GetProperty(SampleProperty).EnumerateArray().Select(id => id.GetGuid()).Should().Equal(anna);
    }

    [Fact]
    public async Task Preview_RecencyFilter_CountsContactsWhoseLastOrderIsWithinTheWindow()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var now = _factory.Clock.GetUtcNow().UtcDateTime;
        var recent = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, new ContactBody("Anna", "Lind", "anna@recency.example.test", null, null, "DE", null, null, null, null), token));
        var old = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, new ContactBody("Bo", "Berg", "bo@recency.example.test", null, null, "DE", null, null, null, null), token));
        await CrmApi.CreateContactAsync(admin, new ContactBody("Chloe", "Dubois", "chloe@recency.example.test", null, null, "FR", null, null, null, null), token);
        await SetLastOrderAsync(recent, now.AddDays(-RecentOrderDays), token);
        await SetLastOrderAsync(old, now.AddDays(-OldOrderDays), token);

        var view = await PreviewAsync(admin, new { daysSinceLastOrder = WindowDays }, token);

        view.GetProperty(CountProperty).GetInt32().Should().Be(1, "only Anna ordered within the window; Bo and Chloe do not qualify");
        view.GetProperty(SampleProperty).EnumerateArray().Select(id => id.GetGuid()).Should().Equal(recent);
    }

    private static async Task<JsonElement> PreviewAsync(HttpClient admin, object body, CancellationToken token)
    {
        var response = await admin.PostAsJsonAsync(CampaignsRoutes.SegmentPreview, body, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, token);
    }

    private static async Task<Guid> CreateTagAsync(HttpClient admin, string name, CancellationToken token)
    {
        var response = await admin.PostAsJsonAsync(CrmRoutes.Tags, new { name, colorIndex = TagColorIndex }, token);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await AuthTestClient.ReadJsonAsync(response, token)).GetProperty(IdProperty).GetGuid();
    }

    private static async Task AttachTagsAsync(HttpClient admin, Guid contactId, Guid[] tagIds, CancellationToken token)
    {
        var response = await admin.PostAsJsonAsync(IdentityApi.Route(CrmRoutes.ContactTags, contactId), new { tagIds }, token);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetLastOrderAsync(Guid contactId, DateTime lastOrderAt, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(UpdateLastOrderSql, connection);
        command.Parameters.AddWithValue("at", lastOrderAt);
        command.Parameters.AddWithValue("id", contactId);
        (await command.ExecuteNonQueryAsync(token)).Should().Be(1);
    }
}
