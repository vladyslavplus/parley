using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Exports;
using Parley.Crm.Persistence;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Xunit;

namespace Parley.IntegrationTests.Crm.Exports;

/// <summary>
/// Contact CSV exports: the file holds only the requester's scope, formula cells are neutralized,
/// the link expires and cleanup removes the file, a repeated idempotency key reuses the export,
/// and the request is audited.
/// </summary>
public sealed class ContactExportTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string FormulaName = "=cmd|' /C calc'!A0";
    private const string NeutralizedFormulaCell = "'=cmd|";
    private const string RawFormulaCell = ",=cmd|";
    private static readonly TimeSpan LinkLifetimeBeyondExpiry = TimeSpan.FromHours(2);

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
    public async Task Export_HoldsOnlyTheRequesterScope_AndNeutralizesFormulaCells()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var repA = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var repB = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        var manager = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesManagerRoleId, token);
        await CrmApi.CreateTeamAsync(_factory, admin, owner.OrganizationId, [repA.UserId, manager.UserId], token);

        var formulaBody = ContactFaker.CreateOwnedBy(repA.UserId);
        formulaBody = formulaBody with { FirstName = FormulaName };
        var inTeam = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, formulaBody, token));
        var outsideTeam = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, ContactFaker.CreateOwnedBy(repB.UserId), token));

        using var repClient = CrmApi.Client(_factory, repA.AccessToken);
        (await ContactExportApi.RequestAsync(repClient, null, new { }, token))
            .StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var managerClient = CrmApi.Client(_factory, manager.AccessToken);
        var exportId = await ContactExportApi.ExportIdOfAsync(
            await ContactExportApi.RequestAsync(managerClient, null, new { }, token),
            token);
        await ContactExportApi.WaitForCompletedAsync(managerClient, exportId, token);

        var download = await managerClient.GetAsync(ContactExportApi.DownloadRoute(exportId), token);
        download.StatusCode.Should().Be(HttpStatusCode.OK);
        var csv = await download.Content.ReadAsStringAsync(token);
        csv.Should().Contain(inTeam.ToString("D"));
        csv.Should().NotContain(outsideTeam.ToString("D"));
        csv.Should().Contain(NeutralizedFormulaCell);
        csv.Should().NotContain(RawFormulaCell);
    }

    [Fact]
    public async Task Export_LinkExpires_AndCleanupRemovesFileAndRow()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        await CrmApi.CreateContactAsync(admin, ContactFaker.Create(), token);
        var exportId = await ContactExportApi.ExportIdOfAsync(
            await ContactExportApi.RequestAsync(admin, null, new { }, token),
            token);
        await ContactExportApi.WaitForCompletedAsync(admin, exportId, token);

        var fileName = await ReadFileNameAsync(exportId, token);
        using (var scope = _factory.Services.CreateScope())
        {
            var storage = scope.ServiceProvider.GetRequiredService<ContactExportStorage>();
            File.Exists(storage.PathFor(fileName)).Should().BeTrue();
        }

        // The host clock moves the export's expiry only. Tokens issued after the jump would carry a future
        // start time that the real-clock validator rejects, so the client signed in before the jump is reused.
        _factory.Clock.Advance(LinkLifetimeBeyondExpiry);
        var expired = await admin.GetAsync(ContactExportApi.DownloadRoute(exportId), token);
        expired.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(expired, token)).Should().Be(CrmErrorCodes.ContactExportExpired);

        using var cleanupScope = _factory.Services.CreateScope();
        var cleanup = ActivatorUtilities.CreateInstance<ContactExportCleanupJob>(cleanupScope.ServiceProvider);
        await cleanup.ExecuteAsync(null, token);

        var storageAfter = cleanupScope.ServiceProvider.GetRequiredService<ContactExportStorage>();
        File.Exists(storageAfter.PathFor(fileName)).Should().BeFalse();
        (await CountExportsAsync(owner.OrganizationId, token)).Should().Be(0);
    }

    [Fact]
    public async Task Export_IdempotencyKeyReuse_ReturnsTheSameExport()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var key = Guid.NewGuid().ToString("N");

        var first = await ContactExportApi.ExportIdOfAsync(
            await ContactExportApi.RequestAsync(admin, key, new { }, token),
            token);
        var second = await ContactExportApi.ExportIdOfAsync(
            await ContactExportApi.RequestAsync(admin, key, new { }, token),
            token);

        second.Should().Be(first);
        (await CountExportsAsync(owner.OrganizationId, token)).Should().Be(1);
    }

    [Fact]
    public async Task Export_IsAudited_AfterTheOutboxIsDelivered()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var exportId = await ContactExportApi.ExportIdOfAsync(
            await ContactExportApi.RequestAsync(admin, null, new { }, token),
            token);

        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        using var scope = _factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var actions = await identity.AuditEvents
            .IgnoreQueryFilters()
            .Where(audit => audit.TargetId == exportId)
            .Select(audit => audit.Action)
            .ToListAsync(token);
        actions.Should().ContainSingle(action => action == CrmAuditActions.ContactsExported);
    }

    [Fact]
    public async Task Export_IsVisibleOnlyToItsRequester()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var exportId = await ContactExportApi.ExportIdOfAsync(
            await ContactExportApi.RequestAsync(admin, null, new { }, token),
            token);
        await ContactExportApi.WaitForCompletedAsync(admin, exportId, token);

        // A marketer of the same organization holds contacts.export too, so only the requester check can refuse.
        var marketer = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.MarketerRoleId, token);
        using var marketerClient = CrmApi.Client(_factory, marketer.AccessToken);

        (await marketerClient.GetAsync(ContactExportApi.ExportRoute(exportId), token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await marketerClient.GetAsync(ContactExportApi.DownloadRoute(exportId), token))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<string> ReadFileNameAsync(Guid exportId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var fileName = await db.ContactExports.IgnoreQueryFilters()
            .Where(export => export.Id == exportId)
            .Select(export => export.FileName)
            .SingleAsync(cancellationToken);
        return fileName ?? throw new InvalidOperationException("The completed export has no file name.");
    }

    private async Task<int> CountExportsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        return await db.ContactExports.IgnoreQueryFilters()
            .CountAsync(export => export.OrganizationId == organizationId, cancellationToken);
    }
}
