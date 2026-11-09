using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>Attaching a tag is idempotent: a duplicate attach, even two in flight at once, leaves one link.</summary>
public sealed class ContactTagAttachTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task AttachingOneTwiceAtOnce_LeavesOneLink()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var tagResponse = await admin.PostAsJsonAsync(CrmRoutes.Tags, new { name = "priority", colorIndex = (short)1 }, token);
        tagResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var tagId = (await AuthTestClient.ReadJsonAsync(tagResponse, token)).GetProperty(IdProperty).GetGuid();
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token));

        var route = IdentityApi.Route(CrmRoutes.ContactTags, contactId);
        var body = new { tagIds = new[] { tagId } };
        var responses = await Task.WhenAll(
            admin.PostAsJsonAsync(route, body, token),
            admin.PostAsJsonAsync(route, body, token));

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var links = await db.ContactTags.IgnoreQueryFilters()
            .CountAsync(link => link.ContactId == contactId && link.TagId == tagId, token);
        links.Should().Be(1);
    }
}
