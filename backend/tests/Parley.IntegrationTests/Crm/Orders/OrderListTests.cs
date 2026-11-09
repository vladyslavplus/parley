using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Contacts;
using Parley.Crm.Persistence;
using Parley.IntegrationTests.Crm.DemoSeed.Support;
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

    private const string FirstCustomerRef = "101";

    private const int FirstCustomerInvoiceCount = 3;

    private IdentityApiFactory _factory = null!;

    private string _directory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(postgres, mailpit);
        _directory = DemoExportFiles.WriteSample(Path.Combine(Path.GetTempPath(), "parley-orders-" + Guid.CreateVersion7().ToString("N")));
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }

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
        items.Should().HaveCount(FirstCustomerInvoiceCount);
        items[0].GetProperty("totalAmount").GetDecimal().Should().Be(-20m);

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
        var importer = scope.ServiceProvider.GetRequiredService<ICrmDemoDataImporter>();
        await importer.ImportAsync(
            new CrmDemoImportRequest(organizationId, _directory, CrmDemoContract.ExportCurrency, CultureCodes.Ukrainian, [Guid.CreateVersion7()]),
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
