using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// Contact views carry the owner name and email from the user directory and the company name from the
/// company projection. The company name is limited to companies the caller may read.
/// </summary>
public sealed class ContactDisplayFieldsTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string CompanyIdProperty = "companyId";
    private const string OwnerDisplayNameProperty = "ownerDisplayName";
    private const string OwnerEmailProperty = "ownerEmail";
    private const string CompanyNameProperty = "companyName";
    private const string CompanyName = "Acme Ltd";
    private const string OwnerName = "Admin Owner";
    private const string RepName = "Rep Member";
    private const int ListLimit = 50;
    private const int ExtraOwnerCount = 4;

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
    public async Task ContactCreateListAndDetail_CarryOwnerNameEmailAndCompanyName()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, owner.UserId, OwnerName, token);
        var (repId, repEmail) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        await IdentitySetup.SetDisplayNameAsync(_factory, repId, RepName, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var companyId = await CreateCompanyAsync(admin, owner.UserId, token);

        var created = await CrmApi.CreateContactAsync(
            admin,
            ContactFaker.CreateOwnedBy(owner.UserId) with { CompanyId = companyId },
            token);
        var ownedContact = CrmApi.IdOf(created);
        AssertOwnerAndCompany(created, owner.Email, OwnerName, CompanyName);

        var repContact = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repId), token));

        var detail = await GetJsonAsync(admin, CrmApi.ContactRoute(ownedContact), token);
        AssertOwnerAndCompany(detail, owner.Email, OwnerName, CompanyName);

        var list = await GetJsonAsync(admin, $"{CrmRoutes.Contacts}?limit={ListLimit}", token);
        AssertOwnerAndCompany(ItemById(list, ownedContact), owner.Email, OwnerName, CompanyName);
        var repRow = ItemById(list, repContact);
        repRow.GetProperty(OwnerDisplayNameProperty).GetString().Should().Be(RepName);
        repRow.GetProperty(OwnerEmailProperty).GetString().Should().Be(repEmail);
        repRow.GetProperty(CompanyNameProperty).ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task CompanyName_IsHidden_FromCallerWhoCannotSeeTheCompany()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var (repBId, _) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var companyOfB = await CreateCompanyAsync(admin, repBId, token);
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(
            admin,
            ContactFaker.CreateOwnedBy(repA.UserId) with { CompanyId = companyOfB },
            token));

        using var repAClient = CrmApi.Client(_factory, repA.AccessToken);
        var view = await GetJsonAsync(repAClient, CrmApi.ContactRoute(contactId), token);

        view.GetProperty(CompanyIdProperty).GetGuid().Should().Be(companyOfB);
        view.GetProperty(CompanyNameProperty).ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ContactList_RunsTheSameStatementsForOneOwnerAndManyOwners()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(owner.UserId), token);
        await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(owner.UserId), token);
        var oneOwnerStatements = await CountListStatementsAsync(admin, token);

        for (var index = 0; index < ExtraOwnerCount; index++)
        {
            var (userId, _) = await IdentitySetup.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
            await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(userId), token);
        }

        var manyOwnerStatements = await CountListStatementsAsync(admin, token);

        manyOwnerStatements.Should().Be(oneOwnerStatements);
    }

    private static string ListRoute => $"{CrmRoutes.Contacts}?limit={ListLimit}";

    private static async Task<int> CountListStatementsAsync(HttpClient client, CancellationToken cancellationToken)
    {
        // The first request in a fresh scope can load the role permission cache; it is not measured.
        (await client.GetAsync(ListRoute, cancellationToken)).StatusCode.Should().Be(HttpStatusCode.OK);

        using var recorder = new SqlStatementRecorder();
        var response = await client.GetAsync(ListRoute, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return recorder.Statements.Count;
    }

    private static void AssertOwnerAndCompany(JsonElement view, string ownerEmail, string ownerName, string? companyName)
    {
        view.GetProperty(OwnerDisplayNameProperty).GetString().Should().Be(ownerName);
        view.GetProperty(OwnerEmailProperty).GetString().Should().Be(ownerEmail);
        view.GetProperty(CompanyNameProperty).GetString().Should().Be(companyName);
    }

    private static JsonElement ItemById(JsonElement page, Guid id) =>
        page.GetProperty(ItemsProperty).EnumerateArray().Single(item => item.GetProperty(IdProperty).GetGuid() == id);

    private static async Task<JsonElement> GetJsonAsync(HttpClient client, string route, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(route, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    private static async Task<Guid> CreateCompanyAsync(HttpClient client, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(CrmRoutes.Companies, new { name = CompanyName, ownerUserId }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return CrmApi.IdOf(await AuthTestClient.ReadJsonAsync(response, cancellationToken));
    }
}
