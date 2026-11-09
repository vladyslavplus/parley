using System.Net;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Persistence;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Pipelines;

/// <summary>
/// An organization that was created before the default pipeline existed gets one on first use. Concurrent first
/// requests still leave exactly one default pipeline with its six stages.
/// </summary>
public sealed class LazyDefaultPipelineTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const int ParallelRequests = 5;
    private const int DefaultStageCount = 6;

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
    public async Task PreExistingOrganization_GetsOneDefaultPipeline_OnFirstUse_EvenWhenRequestsRace()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        await DeleteCrmPipelinesAsync(owner.OrganizationId, token);
        (await CountDefaultPipelinesAsync(owner.OrganizationId, token)).Should().Be(0);

        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var first = await admin.GetAsync(CrmRoutes.Pipelines + "?limit=10", token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        (await CountDefaultPipelinesAsync(owner.OrganizationId, token)).Should().Be(1);

        await DeleteCrmPipelinesAsync(owner.OrganizationId, token);
        var requests = Enumerable.Range(0, ParallelRequests)
            .Select(_ => admin.GetAsync(CrmRoutes.Pipelines + "?limit=10", token))
            .ToArray();
        var responses = await Task.WhenAll(requests);
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        (await CountDefaultPipelinesAsync(owner.OrganizationId, token)).Should().Be(1);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(admin, token);
        var stages = await PipelineApi.StagesAsync(admin, pipelineId, token);
        stages.Should().HaveCount(DefaultStageCount);
    }

    private async Task DeleteCrmPipelinesAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            await db.Pipelines.ExecuteDeleteAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    private async Task<int> CountDefaultPipelinesAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            return await db.Pipelines.CountAsync(pipeline => pipeline.IsDefault, cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }
}
