using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Persistence;
using Parley.Identity.Authorization.Seed;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Xunit;

namespace Parley.IntegrationTests.Crm.Deals;

/// <summary>
/// Undo of a deal delete: a deleted deal comes back with its data and an audit entry, a live deal refuses the restore,
/// and a caller without delete rights or outside the organization cannot restore it.
/// </summary>
public sealed class DealRestoreTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
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
    public async Task Restore_BringsADeletedDealBack_AndRefusesALiveOne()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var deal = await DealApi.CreateAsync(admin, "Restore me", 42m, null, null, token);
        var dealId = DealApi.IdOf(deal);
        (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.DealById, dealId), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await admin.GetAsync(PipelineApi.Route(CrmRoutes.DealById, dealId), token)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var restored = await admin.PostAsync(PipelineApi.Route(CrmRoutes.DealRestore, dealId), null, token);

        restored.StatusCode.Should().Be(HttpStatusCode.OK);
        var view = await AuthTestClient.ReadJsonAsync(restored, token);
        view.GetProperty("title").GetString().Should().Be("Restore me");
        view.GetProperty("amount").GetDecimal().Should().Be(42m);
        (await DealApi.ReadAsync(admin, dealId, token)).GetProperty("id").GetGuid().Should().Be(dealId);

        var again = await admin.PostAsync(PipelineApi.Route(CrmRoutes.DealRestore, dealId), null, token);
        again.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(again, token)).Should().Be(CrmErrorCodes.DealNotDeleted);

        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);
        (await ReadAuditActionsAsync(dealId, token)).Should().Contain(CrmAuditActions.DealRestored);
    }

    [Fact]
    public async Task Restore_IsNotFound_ForAnotherOrganization_AndForbidden_WithoutDeleteRight()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var deal = await DealApi.CreateAsync(admin, "Private", 10m, null, null, token);
        var dealId = DealApi.IdOf(deal);
        (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.DealById, dealId), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var stranger = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var other = CrmApi.Client(_factory, stranger.AccessToken);
        var foreign = await other.PostAsync(PipelineApi.Route(CrmRoutes.DealRestore, dealId), null, token);
        foreign.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(foreign, token)).Should().Be(CrmErrorCodes.DealNotFound);

        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);
        var refused = await repClient.PostAsync(PipelineApi.Route(CrmRoutes.DealRestore, dealId), null, token);
        refused.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<List<string>> ReadAuditActionsAsync(Guid dealId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.AuditEvents
            .IgnoreQueryFilters()
            .Where(audit => audit.TargetId == dealId)
            .Select(audit => audit.Action)
            .ToListAsync(cancellationToken);
    }
}
