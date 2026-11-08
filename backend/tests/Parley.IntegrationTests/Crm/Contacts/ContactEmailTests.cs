using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// Emails are unique among live contacts, ignoring case. A deleted contact releases its email,
/// and restoring it fails while another live contact holds the email.
/// </summary>
public sealed class ContactEmailTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task DuplicateEmail_IsRejected_AndReleasedBySoftDelete()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var original = ContactFaker.Create();
        var first = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, original, token));

        var duplicate = await admin.PostAsJsonAsync(CrmRoutes.Contacts, original with { Email = original.Email!.ToUpperInvariant() }, token);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(duplicate, token)).Should().Be(CrmErrorCodes.ContactEmailTaken);

        (await admin.DeleteAsync(CrmApi.ContactRoute(first), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var reused = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, original, token));

        var restore = await admin.PostAsync(IdentityApi.Route(CrmRoutes.ContactRestore, first), null, token);
        restore.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(restore, token)).Should().Be(CrmErrorCodes.ContactEmailTaken);

        (await admin.DeleteAsync(CrmApi.ContactRoute(reused), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var restored = await admin.PostAsync(IdentityApi.Route(CrmRoutes.ContactRestore, first), null, token);
        restored.StatusCode.Should().Be(HttpStatusCode.OK);
        (await restored.Content.ReadAsStringAsync(token)).Should().Contain(original.Email!);
    }
}
