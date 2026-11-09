using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// Bulk actions respect the caller's scope as a whole: one out-of-scope id fails the batch and changes
/// nothing, and an owner outside the scope is refused with a field error.
/// </summary>
public sealed class ContactBulkScopeTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string OwnerField = "OwnerUserId";

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
    public async Task BulkDelete_WithAnOutOfScopeId_FailsAndDeletesNothing()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var manager = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesManagerRoleId, token);
        await CrmApi.CreateTeamAsync(_factory, admin, owner.OrganizationId, [repA.UserId, manager.UserId], token);
        var ownedByA = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repA.UserId), token));
        var ownedByB = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repB.UserId), token));

        using var managerClient = CrmApi.Client(_factory, manager.AccessToken);
        var response = await managerClient.PostAsJsonAsync(CrmRoutes.ContactsBulkDelete, new { ids = new[] { ownedByA, ownedByB } }, token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.GetAsync(CrmApi.ContactRoute(ownedByA), token)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync(CrmApi.ContactRoute(ownedByB), token)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BulkOwner_ToAnOwnerOutsideTheScope_ReturnsAFieldError()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var manager = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesManagerRoleId, token);
        await CrmApi.CreateTeamAsync(_factory, admin, owner.OrganizationId, [repA.UserId, manager.UserId], token);
        var ownedByA = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repA.UserId), token));

        using var managerClient = CrmApi.Client(_factory, manager.AccessToken);
        var response = await managerClient.PostAsJsonAsync(
            CrmRoutes.ContactsBulkOwner,
            new { ids = new[] { ownedByA }, ownerUserId = repB.UserId },
            token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, OwnerField, token)).Should().NotBeNullOrEmpty();
    }
}
