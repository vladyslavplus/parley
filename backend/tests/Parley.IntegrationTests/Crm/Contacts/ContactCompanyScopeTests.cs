using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Errors;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// A contact can link only to a company the caller may see. A company owned by someone else is reported
/// as missing, the same as one that does not exist.
/// </summary>
public sealed class ContactCompanyScopeTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string CompanyField = "CompanyId";
    private const string CompanyName = "Acme Ltd";

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
    public async Task RepCannotLinkToAnUnseenCompany_ButCanLinkToOwnCompany()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var companyOfB = await CreateCompanyAsync(admin, repB.UserId, token);
        var companyOfA = await CreateCompanyAsync(admin, repA.UserId, token);

        using var repAClient = CrmApi.Client(_factory, repA.AccessToken);
        var unseen = await repAClient.PostAsJsonAsync(
            CrmRoutes.Contacts,
            ContactFaker.CreateOwnedBy(repA.UserId) with { CompanyId = companyOfB },
            token);
        unseen.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(unseen, CompanyField, token)).Should().Be(CrmErrorCodes.ContactCompanyNotFound);

        var own = await repAClient.PostAsJsonAsync(
            CrmRoutes.Contacts,
            ContactFaker.CreateOwnedBy(repA.UserId) with { CompanyId = companyOfA },
            token);
        own.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<Guid> CreateCompanyAsync(HttpClient admin, Guid ownerUserId, CancellationToken cancellationToken)
    {
        var response = await admin.PostAsJsonAsync(CrmRoutes.Companies, new { name = CompanyName, ownerUserId }, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return CrmApi.IdOf(await AuthTestClient.ReadJsonAsync(response, cancellationToken));
    }
}
