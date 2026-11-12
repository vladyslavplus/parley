using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Orders;

/// <summary>The order endpoints list a contact's invoices and hide them from other organizations.</summary>
public sealed class OrderListTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ItemsProperty = "items";

    private const string ContactIdProperty = "contactId";

    private const string ErrorCodeProperty = "code";

    private const string FirstCustomerRef = "000000";

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
    public async Task ListsTheInvoicesOfOneContactAndReadsOneInvoice()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await ImportIntoAsync(owner.OrganizationId, token);
        var contactId = await ContactIdAsync(owner.OrganizationId, FirstCustomerRef, token);
        using var client = CrmApi.Client(_factory, owner.AccessToken);

        using var listResponse = await client.GetAsync($"orders?{ContactIdProperty}={contactId:D}&sort=amount&order=asc", token);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync(token));
        var items = list.RootElement.GetProperty(ItemsProperty).EnumerateArray().ToArray();
        items.Should().NotBeEmpty();
        items.Select(item => item.GetProperty("totalAmount").GetDecimal())
            .Should().BeInAscendingOrder("the amount sort is ascending");

        var invoiceId = items[0].GetProperty("id").GetGuid();
        using var getResponse = await client.GetAsync($"orders/{invoiceId:D}", token);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnotherOrganizationGetsNotFoundForAnInvoice()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var other = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await ImportIntoAsync(owner.OrganizationId, token);
        var contactId = await ContactIdAsync(owner.OrganizationId, FirstCustomerRef, token);
        using var ownerClient = CrmApi.Client(_factory, owner.AccessToken);
        using var listResponse = await ownerClient.GetAsync($"orders?{ContactIdProperty}={contactId:D}", token);
        using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync(token));
        var invoiceId = list.RootElement.GetProperty(ItemsProperty)[0].GetProperty("id").GetGuid();

        using var otherClient = CrmApi.Client(_factory, other.AccessToken);
        using var response = await otherClient.GetAsync($"orders/{invoiceId:D}", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
        problem.RootElement.GetProperty(ErrorCodeProperty).GetString().Should().Be(CrmErrorCodes.OrderNotFound);
    }

    private async Task ImportIntoAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var profile = new CrmDemoProfile
        {
            Contacts = 3,
            OrderMonths = 6,
            DealSharePercent = 0,
            TaskSharePercent = 0,
            NoteSharePercent = 0,
            TagSharePercent = 0,
            NoOrdersPercent = 0,
        };
        await scope.ServiceProvider.GetRequiredService<ICrmDemoSeeder>().SeedAsync(
            new CrmDemoImportRequest(
                organizationId,
                CrmDemoContract.DemoCurrency,
                CultureCodes.Ukrainian,
                [Guid.CreateVersion7()],
                5,
                _factory.Clock.GetUtcNow(),
                profile),
            cancellationToken);
    }

    private async Task<Guid> ContactIdAsync(Guid organizationId, string externalRef, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMutableTenantContext>().Set(organizationId);
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        return await db.Contacts.AsNoTracking()
            .Where(contact => contact.Source == ContactSources.DemoSeed && contact.ExternalRef == externalRef)
            .Select(contact => contact.Id)
            .SingleAsync(cancellationToken);
    }
}
