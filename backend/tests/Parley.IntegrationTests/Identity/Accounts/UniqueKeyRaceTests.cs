using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Identity;
using Parley.Identity.Contracts.Errors;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.IntegrationTests.Identity.Accounts;

/// <summary>
/// Parallel requests that claim the same unique key get one success and one conflict,
/// never a server error.
/// </summary>
public sealed class UniqueKeyRaceTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ContestedTeamName = "Parallel team";

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
    public async Task ParallelCreatesOfTheSameTeamName_OneSucceeds_TheOtherIsConflict()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.English, owner.AccessToken);

        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync(IdentityRoutes.Teams, new { name = ContestedTeamName }, token),
            admin.PostAsJsonAsync(IdentityRoutes.Teams, new { name = ContestedTeamName }, token));

        responses.Select(response => response.StatusCode).Should()
            .BeEquivalentTo(new[] { HttpStatusCode.OK, HttpStatusCode.Conflict });
        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await AuthTestClient.ReadJsonAsync(conflict, token)).GetProperty("code").GetString()
            .Should().Be(IdentityErrorCodes.TeamNameTaken);
    }
}
