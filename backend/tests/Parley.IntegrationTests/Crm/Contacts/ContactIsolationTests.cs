using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>A contact of one organization is invisible to another: reads, writes and deletes answer not found.</summary>
public sealed class ContactIsolationTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task OtherOrganization_GetsNotFound_OnReadUpdateAndDelete()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var ownerClient = CrmApi.Client(_factory, owner.AccessToken);
        using var otherClient = CrmApi.Client(_factory, other.AccessToken);
        var contact = await CrmApi.CreateContactAsync(ownerClient, ContactFaker.Create(), token);
        var route = CrmApi.ContactRoute(CrmApi.IdOf(contact));

        (await otherClient.GetAsync(route, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.PutAsJsonAsync(route, new { version = CrmApi.VersionOf(contact), firstName = "Mallory" }, token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await otherClient.DeleteAsync(route, token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
