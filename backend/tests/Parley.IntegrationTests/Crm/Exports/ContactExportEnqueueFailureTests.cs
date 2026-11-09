using System.Net;
using AwesomeAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Domain.Exports;
using Parley.Crm.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Crm.Exports;

/// <summary>
/// The export row and its audit event commit before the job is queued. When the queue refuses the job, the
/// row is marked failed and the caller gets a stable error, so nothing is left queued with no job behind it.
/// </summary>
public sealed class ContactExportEnqueueFailureTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraServices: services =>
            {
                services.RemoveAll<IBackgroundJobClient>();
                services.AddSingleton<IBackgroundJobClient>(provider => new ExportEnqueueFailingJobClient(
                    new BackgroundJobClient(provider.GetRequiredService<JobStorage>())));
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task EnqueueFailure_MarksTheCommittedExportFailed_AndReturnsStableError()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);

        var response = await ContactExportApi.RequestAsync(admin, null, new { }, token);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await CrmApi.ErrorCodeAsync(response, token)).Should().Be(CrmErrorCodes.ContactExportEnqueueFailed);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var statuses = await db.ContactExports.IgnoreQueryFilters()
            .Where(export => export.OrganizationId == owner.OrganizationId)
            .Select(export => export.Status)
            .ToListAsync(token);
        statuses.Should().ContainSingle().Which.Should().Be(ContactExportStatuses.Failed);
    }
}
