using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Versions;

/// <summary>
/// An update sends the row version the client read. A stale version answers 409 with a campaigns code, never a server
/// error, and the version returned by an update is the one the next update must send.
/// </summary>
public sealed class VersionConflictTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string IdProperty = "id";

    private const string VersionProperty = "version";

    private const string NameProperty = "name";

    private const string BodyLiquid = "Hello {{ contact.first_name }}";

    private const string SubjectLiquid = "Welcome";

    private const string Locale = "en";

    private const string CountryCode = "DE";

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
    public async Task TemplateUpdate_WithStaleVersion_AnswersConflictWithCampaignsCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var created = await admin.PostAsJsonAsync(CampaignsRoutes.Templates, new { name = "Welcome", subjectLiquid = SubjectLiquid, bodyLiquid = BodyLiquid, locale = Locale }, token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var template = await AuthTestClient.ReadJsonAsync(created, token);
        var id = template.GetProperty(IdProperty).GetGuid();
        var route = IdentityApi.Route(CampaignsRoutes.TemplateById, id);
        var readVersion = template.GetProperty(VersionProperty).GetUInt32();

        var first = await admin.PutAsJsonAsync(route, new { name = "Welcome v2", subjectLiquid = SubjectLiquid, bodyLiquid = BodyLiquid, locale = Locale, version = readVersion }, token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await AuthTestClient.ReadJsonAsync(first, token);
        var freshVersion = updated.GetProperty(VersionProperty).GetUInt32();
        freshVersion.Should().NotBe(readVersion, "the save changed the row");

        var stale = await admin.PutAsJsonAsync(route, new { name = "Lost update", subjectLiquid = SubjectLiquid, bodyLiquid = BodyLiquid, locale = Locale, version = readVersion }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await AuthTestClient.ReadJsonAsync(stale, token);
        problem.GetProperty(ApiConstants.ProblemCode).GetString().Should().Be(CampaignsErrorCodes.TemplateVersionConflict);

        var current = await admin.GetAsync(route, token);
        (await AuthTestClient.ReadJsonAsync(current, token)).GetProperty(NameProperty).GetString().Should().Be("Welcome v2", "the stale write changed nothing");

        var next = await admin.PutAsJsonAsync(route, new { name = "Welcome v3", subjectLiquid = SubjectLiquid, bodyLiquid = BodyLiquid, locale = Locale, version = freshVersion }, token);
        next.StatusCode.Should().Be(HttpStatusCode.OK, "the version returned by the previous update is current");
    }

    [Fact]
    public async Task SegmentUpdate_WithStaleVersion_AnswersConflictWithCampaignsCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var created = await admin.PostAsJsonAsync(CampaignsRoutes.Segments, new { name = "Germany", definition = new { countryCodes = new[] { CountryCode } } }, token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var segment = await AuthTestClient.ReadJsonAsync(created, token);
        var id = segment.GetProperty(IdProperty).GetGuid();
        var route = IdentityApi.Route(CampaignsRoutes.SegmentById, id);
        var readVersion = segment.GetProperty(VersionProperty).GetUInt32();

        var first = await admin.PutAsJsonAsync(route, new { name = "Germany v2", definition = new { countryCodes = new[] { CountryCode } }, version = readVersion }, token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var freshVersion = (await AuthTestClient.ReadJsonAsync(first, token)).GetProperty(VersionProperty).GetUInt32();

        var stale = await admin.PutAsJsonAsync(route, new { name = "Lost update", definition = new { }, version = readVersion }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await AuthTestClient.ReadJsonAsync(stale, token);
        problem.GetProperty(ApiConstants.ProblemCode).GetString().Should().Be(CampaignsErrorCodes.SegmentVersionConflict);

        var next = await admin.PutAsJsonAsync(route, new { name = "Germany v3", definition = new { countryCodes = new[] { CountryCode } }, version = freshVersion }, token);
        next.StatusCode.Should().Be(HttpStatusCode.OK, "the version returned by the previous update is current");
    }
}
