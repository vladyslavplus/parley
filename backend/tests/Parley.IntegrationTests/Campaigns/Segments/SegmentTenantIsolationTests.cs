using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Campaigns;
using Parley.Campaigns.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Infrastructure.Paging;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Segments;

/// <summary>A segment belongs to its organization: another organization can neither read it nor list it.</summary>
public sealed class SegmentTenantIsolationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string IdProperty = "id";

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
    public async Task Segment_OfAnotherOrganization_IsNotFoundAndNotListed()
    {
        var token = TestContext.Current.CancellationToken;
        var first = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var second = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var firstClient = CrmApi.Client(_factory, first.AccessToken);
        using var secondClient = CrmApi.Client(_factory, second.AccessToken);

        var created = await firstClient.PostAsJsonAsync(CampaignsRoutes.Segments, new { name = "Germany", definition = new { countryCodes = new[] { "DE" } } }, token);
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var segmentId = (await AuthTestClient.ReadJsonAsync(created, token)).GetProperty(IdProperty).GetGuid();

        var foreignRead = await secondClient.GetAsync(IdentityApi.Route(CampaignsRoutes.SegmentById, segmentId), token);
        foreignRead.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await AuthTestClient.ReadJsonAsync(foreignRead, token);
        problem.GetProperty(Parley.Api.ApiConstants.ProblemCode).GetString().Should().Be(CampaignsErrorCodes.SegmentNotFound);

        var foreignList = await ListPages.ReadAllAsync(secondClient, CampaignsRoutes.Segments, token);
        foreignList.Select(row => row.GetProperty(IdProperty).GetGuid()).Should().NotContain(segmentId);

        var ownList = await ListPages.ReadAllAsync(firstClient, CampaignsRoutes.Segments, token);
        ownList.Select(row => row.GetProperty(IdProperty).GetGuid()).Should().Contain(segmentId);
    }
}
