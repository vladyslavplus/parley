using System.Net;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Search;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Search;

/// <summary>
/// Global search finds a record despite a transposed letter, and returns only records the caller's
/// scope allows in the caller's own organization.
/// </summary>
public sealed class ContactSearchTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string TransposedTerm = "mirima";
    private const string SharedLastName = "Shevchenko";
    private const string ContactKind = SearchResultKinds.Contact;
    private const string ItemsProperty = "items";
    private const string KindProperty = "kind";
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
    public async Task Search_FindsTheRecord_ForATransposedLetter()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var body = ContactFaker.Create() with { FirstName = "Miriam", LastName = "Okafor" };
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, body, token));

        var ids = await SearchIdsAsync(admin, TransposedTerm, token);

        ids.Should().ContainSingle(hit => hit.Kind == ContactKind && hit.Id == contactId);
    }

    [Fact]
    public async Task Search_ReturnsOnlyRecordsInScopeAndOrganization()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var ownedByA = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repA.UserId) with { LastName = SharedLastName }, token));
        var ownedByB = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repB.UserId) with { LastName = SharedLastName }, token));

        var otherOwner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var otherAdmin = CrmApi.Client(_factory, otherOwner.AccessToken);
        await CrmApi.CreateContactAsync(otherAdmin, ContactFaker.Create() with { LastName = SharedLastName }, token);

        using var repAClient = CrmApi.Client(_factory, repA.AccessToken);
        var repView = await SearchIdsAsync(repAClient, SharedLastName, token);
        repView.Select(hit => hit.Id).Should().BeEquivalentTo([ownedByA]);

        var adminView = await SearchIdsAsync(admin, SharedLastName, token);
        adminView.Select(hit => hit.Id).Should().BeEquivalentTo([ownedByA, ownedByB]);

        var otherView = await SearchIdsAsync(otherAdmin, SharedLastName, token);
        otherView.Select(hit => hit.Id).Should().ContainSingle();
        otherView.Should().NotContain(hit => hit.Id == ownedByA || hit.Id == ownedByB);
    }

    private static async Task<List<(string Kind, Guid Id)>> SearchIdsAsync(
        HttpClient client,
        string term,
        CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{CrmRoutes.Search}?q={Uri.EscapeDataString(term)}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray()
            .Select(item => (item.GetProperty(KindProperty).GetString()!, item.GetProperty(IdProperty).GetGuid()))
            .ToList();
    }
}
