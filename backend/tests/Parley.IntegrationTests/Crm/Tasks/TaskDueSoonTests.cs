using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Persistence;
using Parley.Crm.Tasks;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Tasks;

/// <summary>
/// The due-soon sweep queues one job per organization with a reminder due. Each job announces every open task in its
/// window exactly once: a re-run adds nothing, and a completed or distant task is skipped.
/// </summary>
public sealed class TaskDueSoonTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private static readonly string TaskDueSoonType = typeof(TaskDueSoon).FullName!;

    private IdentityApiFactory _factory = null!;
    private RecordingBackgroundJobClient _jobs = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraServices: services =>
            {
                services.RemoveAll<IBackgroundJobClient>();
                services.AddSingleton<IBackgroundJobClient>(provider =>
                {
                    _jobs = new RecordingBackgroundJobClient(new BackgroundJobClient(provider.GetRequiredService<JobStorage>()));
                    return _jobs;
                });
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Sweep_QueuesOneJobPerOrganization_AndEachJobAnnouncesEachTaskOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var now = _factory.Clock.GetUtcNow();
        var soon = await TaskApi.CreateAsync(admin, "Soon", now.AddMinutes(30), null, null, token);
        await TaskApi.CreateAsync(admin, "Far away", now.AddHours(5), null, null, token);
        var finished = await TaskApi.CreateAsync(admin, "Finished", now.AddMinutes(10), null, null, token);
        (await TaskApi.CompleteAsync(admin, TaskApi.IdOf(finished), TaskApi.VersionOf(finished), token)).EnsureSuccessStatusCode();

        await RunSweepAsync(token);

        var queued = _jobs.Created.Where(job => job.Type == typeof(TaskDueSoonJob)).ToList();
        queued.Should().ContainSingle();
        queued[0].Args.First().Should().Be(owner.OrganizationId);

        await RunJobAsync(owner.OrganizationId, token);
        await RunJobAsync(owner.OrganizationId, token);

        var announced = await ReadTaskDueSoonAsync(owner.OrganizationId, token);
        announced.Should().ContainSingle();
        announced[0].Should().Be(TaskApi.IdOf(soon));
    }

    [Fact]
    public async Task EachJob_RunsUnderItsOwnOrganization_AndNeverAnnouncesAnotherOrganizationsTask()
    {
        var token = TestContext.Current.CancellationToken;
        var first = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var second = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        var due = _factory.Clock.GetUtcNow().AddMinutes(20);
        using var firstClient = CrmApi.Client(_factory, first.AccessToken);
        using var secondClient = CrmApi.Client(_factory, second.AccessToken);
        var firstTask = await TaskApi.CreateAsync(firstClient, "First org", due, null, null, token);
        var secondTask = await TaskApi.CreateAsync(secondClient, "Second org", due, null, null, token);

        await RunJobAsync(first.OrganizationId, token);

        (await ReadTaskDueSoonAsync(first.OrganizationId, token)).Should().Equal(TaskApi.IdOf(firstTask));
        (await ReadTaskDueSoonAsync(second.OrganizationId, token)).Should().BeEmpty();

        await RunJobAsync(second.OrganizationId, token);

        (await ReadTaskDueSoonAsync(second.OrganizationId, token)).Should().Equal(TaskApi.IdOf(secondTask));
        await OutboxDelivery.DeliverDueAsync<CrmDbContext>(_factory.Services, token);
    }

    [Fact]
    public async Task Claim_OnlyTakesAnOpenTaskStillInTheWindow_AndOnlyOnce()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var now = _factory.Clock.GetUtcNow();
        var soon = now.AddMinutes(30);
        var open = await TaskApi.CreateAsync(admin, "Claim me", soon, null, null, token);
        var completed = await TaskApi.CreateAsync(admin, "Completed meanwhile", soon, null, null, token);
        (await TaskApi.CompleteAsync(admin, TaskApi.IdOf(completed), TaskApi.VersionOf(completed), token)).EnsureSuccessStatusCode();
        var deleted = await TaskApi.CreateAsync(admin, "Deleted meanwhile", soon, null, null, token);
        (await admin.DeleteAsync(PipelineApi.Route(CrmRoutes.TaskById, TaskApi.IdOf(deleted)), token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var far = await TaskApi.CreateAsync(admin, "Far away", now.AddHours(5), null, null, token);
        var windowEnd = now.Add(TimeSpan.FromMinutes(60)).UtcDateTime;

        (await ClaimAsync(owner.OrganizationId, TaskApi.IdOf(completed), windowEnd, token)).Should().Be(0);
        (await ClaimAsync(owner.OrganizationId, TaskApi.IdOf(deleted), windowEnd, token)).Should().Be(0);
        (await ClaimAsync(owner.OrganizationId, TaskApi.IdOf(far), windowEnd, token)).Should().Be(0);
        (await ClaimAsync(owner.OrganizationId, TaskApi.IdOf(open), windowEnd, token)).Should().Be(1);
        (await ClaimAsync(owner.OrganizationId, TaskApi.IdOf(open), windowEnd, token)).Should().Be(0);
    }

    private async Task<int> ClaimAsync(Guid organizationId, Guid taskId, DateTime windowEnd, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            return await TaskReminderClaim.ClaimAsync(db, taskId, _factory.Clock.GetUtcNow().UtcDateTime, windowEnd, cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    /// <summary>Runs the platform-wide sweep the way the scheduler does.</summary>
    private async Task RunSweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var sweep = scope.ServiceProvider.GetRequiredService<TaskDueSoonScanJob>();
        await sweep.ExecuteAsync(null, cancellationToken);
    }

    /// <summary>Runs one organization's reminder job the way Hangfire does.</summary>
    private async Task RunJobAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<TaskDueSoonJob>();
        await job.ExecuteAsync(organizationId, cancellationToken);
    }

    private static Guid TaskIdOf(string payload) =>
        JsonDocument.Parse(payload).RootElement.EnumerateObject()
            .First(property => string.Equals(property.Name, "taskId", StringComparison.OrdinalIgnoreCase))
            .Value.GetGuid();

    /// <summary>Ids of the tasks whose reminder event sits in the organization's outbox.</summary>
    private async Task<List<Guid>> ReadTaskDueSoonAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var payloads = await db.Set<OutboxMessage>()
            .Where(message => message.OrganizationId == organizationId && message.Type == TaskDueSoonType)
            .Select(message => message.Payload)
            .ToListAsync(cancellationToken);
        return payloads.Select(TaskIdOf).ToList();
    }
}
