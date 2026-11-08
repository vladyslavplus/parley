using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Audit;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Persistence;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Contacts;

/// <summary>
/// A business change and its events commit together: a rejected create leaves no event behind,
/// and a delete reaches the Identity audit log once the outbox is delivered.
/// </summary>
public sealed class ContactOutboxTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string ContactActivityType = "contact_created";

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
    public async Task RejectedCreate_LeavesNoEvent_AndDeleteReachesAuditLog()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var body = ContactFaker.Create();
        var contactId = CrmApi.IdOf(await CrmApi.CreateContactAsync(admin, body, token));

        var duplicate = await admin.PostAsJsonAsync(CrmRoutes.Contacts, body, token);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var eventTypes = await ReadOutboxTypesAsync(owner.OrganizationId, token);
        eventTypes.Should().ContainSingle(type => type == typeof(ContactCreated).FullName);
        (await ReadActivityTypesAsync(owner.OrganizationId, contactId, token)).Should().Equal(ContactActivityType);

        (await admin.DeleteAsync(CrmApi.ContactRoute(contactId), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);

        var auditActions = await ReadAuditActionsAsync(contactId, token);
        auditActions.Should().ContainSingle(action => action == CrmAuditActions.ContactDeleted);
    }

    private async Task<List<string>> ReadOutboxTypesAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        return await db.Set<OutboxMessage>()
            .Where(message => message.OrganizationId == organizationId)
            .Select(message => message.Type)
            .ToListAsync(cancellationToken);
    }

    private async Task<List<string>> ReadActivityTypesAsync(Guid organizationId, Guid contactId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            return await db.Activities
                .Where(activity => activity.ContactId == contactId)
                .Select(activity => activity.Type)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    private async Task<List<string>> ReadAuditActionsAsync(Guid contactId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        return await db.AuditEvents
            .IgnoreQueryFilters()
            .Where(audit => audit.TargetId == contactId)
            .Select(audit => audit.Action)
            .ToListAsync(cancellationToken);
    }
}
