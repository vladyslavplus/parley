using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Api.Controllers.Crm;
using Parley.Crm.Persistence;
using Parley.Identity.Contracts.Events;
using Parley.Identity.Persistence;
using Parley.IntegrationTests.Access.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.IntegrationTests.Outbox.Support;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Outbox.Consumers;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Pipelines;

/// <summary>
/// A new organization gets exactly one default pipeline. Its system names follow the request language, while a
/// stage the user renamed keeps the typed name in every language. The organization-created event is delivered
/// through the outbox, and replaying it adds nothing.
/// </summary>
public sealed class DefaultPipelineTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string EnglishNewStageName = "New";

    private const string TypedStageName = "Discovery";

    private const int TypedColorIndex = 2;

    private const int TypedProbabilityBps = 1500;

    private static readonly string[] UkrainianStageNames = ["Нові", "Кваліфікація", "Пропозиція", "Переговори", "Виграно", "Програно"];

    private static readonly string[] EnglishStageNames = ["New", "Qualified", "Proposal", "Negotiation", "Won", "Lost"];

    private static readonly string[] StageKinds = ["open", "open", "open", "open", "won", "lost"];

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
    public async Task NewOrganization_GetsOneDefaultPipeline_ShownInTheRequestLanguage_AndReplayAddsNone()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await CreateUkrainianOwnerAsync(token);
        await OutboxDelivery.DeliverDueAsync<IdentityDbContext>(_factory.Services, token);

        (await CountDefaultPipelinesAsync(owner.OrganizationId, token)).Should().Be(1);

        await ReplayOrganizationCreatedAsync(owner.OrganizationId, token);
        (await CountDefaultPipelinesAsync(owner.OrganizationId, token)).Should().Be(1);

        using var english = CrmApi.Client(_factory, owner.AccessToken);
        using var ukrainian = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.Ukrainian, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(english, token);

        var englishStages = await PipelineApi.StagesAsync(english, pipelineId, token);
        englishStages.Select(stage => stage.Name).Should().Equal(EnglishStageNames);
        englishStages.Select(stage => stage.Kind).Should().Equal(StageKinds);

        var ukrainianStages = await PipelineApi.StagesAsync(ukrainian, pipelineId, token);
        ukrainianStages.Select(stage => stage.Name).Should().Equal(UkrainianStageNames);
    }

    [Fact]
    public async Task RenamedStage_KeepsItsTypedName_InEveryLanguage()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await CreateUkrainianOwnerAsync(token);
        await OutboxDelivery.DeliverDueAsync<IdentityDbContext>(_factory.Services, token);

        using var english = CrmApi.Client(_factory, owner.AccessToken);
        using var ukrainian = IdentityApi.CreateAuthorizedClient(_factory, CultureCodes.Ukrainian, owner.AccessToken);
        var pipelineId = await PipelineApi.DefaultPipelineIdAsync(english, token);
        var newStage = PipelineApi.Named(await PipelineApi.StagesAsync(english, pipelineId, token), EnglishNewStageName);

        var renamed = await english.PutAsJsonAsync(
            PipelineApi.Route(CrmRoutes.PipelineStageById, pipelineId, newStage.Id),
            new { name = TypedStageName, colorIndex = TypedColorIndex, probabilityBps = TypedProbabilityBps },
            token);
        renamed.StatusCode.Should().Be(HttpStatusCode.OK);

        string[] expectedEnglish = [TypedStageName, .. EnglishStageNames[1..]];
        string[] expectedUkrainian = [TypedStageName, .. UkrainianStageNames[1..]];
        var englishStages = await PipelineApi.StagesAsync(english, pipelineId, token);
        englishStages.Select(stage => stage.Name).Should().Equal(expectedEnglish);

        var ukrainianStages = await PipelineApi.StagesAsync(ukrainian, pipelineId, token);
        ukrainianStages.Select(stage => stage.Name).Should().Equal(expectedUkrainian);
        PipelineApi.Named(ukrainianStages, TypedStageName).Id.Should().Be(newStage.Id);
    }

    private async Task<OrganizationOwner> CreateUkrainianOwnerAsync(CancellationToken cancellationToken)
    {
        using var client = AuthTestClient.CreateClient(_factory, CultureCodes.Ukrainian);
        var account = await AuthTestClient.CreateConfirmedAccountAsync(client, mailpit, cancellationToken);
        var organizationId = await AccessTestData.ReadOrganizationIdAsync(_factory.Services, account.UserId, cancellationToken);
        var tokens = await IdentityApi.SignInAsync(_factory, account.Email, cancellationToken);
        return new OrganizationOwner(account.Email, account.UserId, organizationId, tokens.AccessToken);
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

    /// <summary>Hands the event to the consumer again, the way the dispatcher does after a retry.</summary>
    private async Task ReplayOrganizationCreatedAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        var consumer = scope.ServiceProvider.GetRequiredService<IIntegrationEventConsumer<OrganizationCreated>>();
        tenant.Set(organizationId);
        try
        {
            await consumer.ConsumeAsync(
                new OrganizationCreated { OrganizationId = organizationId, DefaultLocale = CultureCodes.Ukrainian, OccurredAt = _factory.Clock.GetUtcNow() },
                cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }
}
