using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Errors;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>An update carries the row version it read; a stale version is rejected with a stable code.</summary>
public sealed class ContactVersionTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
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
    public async Task StaleVersion_ReturnsConflict_AfterAnotherWrite()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var created = await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token);
        var route = CrmApi.ContactRoute(CrmApi.IdOf(created));
        var readVersion = CrmApi.VersionOf(created);

        var first = await admin.PutAsJsonAsync(route, new { version = readVersion, firstName = "Ada" }, token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await AuthTestClient.ReadJsonAsync(first, token);
        CrmApi.VersionOf(updated).Should().NotBe(readVersion);

        var stale = await admin.PutAsJsonAsync(route, new { version = readVersion, firstName = "Grace" }, token);
        stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(stale, token)).Should().Be(ErrorCodes.ConcurrencyConflict);
    }
}
