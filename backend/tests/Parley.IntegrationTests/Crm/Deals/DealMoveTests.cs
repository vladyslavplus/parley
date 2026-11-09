using System.Net;
using System.Text.Json;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Parley.Crm.Contracts.Errors;
using Parley.Crm.Contracts.Events;
using Parley.Crm.Domain.Deals;
using Parley.Crm.Persistence;
using Parley.Identity.Authorization.Seed;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Errors;
using Parley.SharedKernel.Outbox.Messages;
using Parley.SharedKernel.Tenancy.Abstractions;
using Xunit;

namespace Parley.IntegrationTests.Crm.Deals;

/// <summary>
/// Moving a deal writes the stage history, the timeline and the events in one transaction, rejects a stale version
/// and an unknown stage, respects the owner scope, and keeps the board order when a gap runs out.
/// </summary>
public sealed class DealMoveTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string NewStageName = "New";
    private const string QualifiedStageName = "Qualified";
    private const string ProposalStageName = "Proposal";
    private const string WonStageName = "Won";
    private const string StatusProperty = "status";
    private const string ClosedAtProperty = "closedAt";
    private const string DealStageChangedType = "deal_stage_changed";
    private const string DealClosedType = "deal_closed";
    private const int CrowdedDealCount = 14;
    private const int FirstMovedIndex = 2;

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
    public async Task Move_ToWon_ClosesTheDeal_AndWritesHistoryTimelineAndEvents()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var newStage = PipelineApi.Named(stages, NewStageName);
        var qualified = PipelineApi.Named(stages, QualifiedStageName);
        var won = PipelineApi.Named(stages, WonStageName);
        var deal = await DealApi.CreateAsync(admin, "Renewal", 1200.50m, newStage.Id, null, token);
        var dealId = DealApi.IdOf(deal);

        var closed = await DealApi.MoveAsync(admin, dealId, won.Id, 0, DealApi.VersionOf(deal), token);
        closed.StatusCode.Should().Be(HttpStatusCode.OK);
        var closedView = await AuthTestClient.ReadJsonAsync(closed, token);
        closedView.GetProperty(StatusProperty).GetString().Should().Be(DealStatuses.Won);
        closedView.GetProperty(ClosedAtProperty).ValueKind.Should().NotBe(JsonValueKind.Null);

        var reopened = await DealApi.MoveAsync(admin, dealId, qualified.Id, 0, DealApi.VersionOf(closedView), token);
        reopened.StatusCode.Should().Be(HttpStatusCode.OK);
        var reopenedView = await AuthTestClient.ReadJsonAsync(reopened, token);
        reopenedView.GetProperty(StatusProperty).GetString().Should().Be(DealStatuses.Open);
        reopenedView.GetProperty(ClosedAtProperty).ValueKind.Should().Be(JsonValueKind.Null);

        (await CountHistoryAsync(owner.OrganizationId, dealId, token)).Should().Be(3);
        (await ReadActivityTypesAsync(owner.OrganizationId, dealId, token))
            .Should().BeEquivalentTo(new[] { DealStageChangedType, DealStageChangedType, DealClosedType });
        var outboxTypes = await ReadOutboxTypesAsync(owner.OrganizationId, token);
        outboxTypes.Count(type => type == typeof(DealStageChanged).FullName).Should().Be(2);
        outboxTypes.Count(type => type == typeof(DealClosed).FullName).Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentMovesFromOneVersion_OneSucceeds_TheOtherIsConflict()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var deal = await DealApi.CreateAsync(admin, "Contested", 10m, PipelineApi.Named(stages, NewStageName).Id, null, token);
        var version = DealApi.VersionOf(deal);

        var responses = await Task.WhenAll(
            DealApi.MoveAsync(admin, DealApi.IdOf(deal), PipelineApi.Named(stages, QualifiedStageName).Id, 0, version, token),
            DealApi.MoveAsync(admin, DealApi.IdOf(deal), PipelineApi.Named(stages, ProposalStageName).Id, 0, version, token));

        responses.Select(response => response.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var conflict = responses.Single(response => response.StatusCode == HttpStatusCode.Conflict);
        (await CrmApi.ErrorCodeAsync(conflict, token)).Should().Be(ErrorCodes.ConcurrencyConflict);
    }

    [Fact]
    public async Task Move_ToAStageOutsideThePipeline_IsRejectedWithAFieldCode()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var deal = await DealApi.CreateAsync(admin, "Misplaced", 10m, PipelineApi.Named(stages, NewStageName).Id, null, token);

        var response = await DealApi.MoveAsync(admin, DealApi.IdOf(deal), Guid.CreateVersion7(), 0, DealApi.VersionOf(deal), token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CrmApi.FieldErrorCodeAsync(response, "stageId", token)).Should().Be(CrmErrorCodes.DealStageNotFound);
    }

    [Fact]
    public async Task Move_OfADealOutsideTheRepScope_IsNotFound()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var deal = await DealApi.CreateAsync(admin, "Admin deal", 10m, PipelineApi.Named(stages, NewStageName).Id, null, token);
        var rep = await CrmApi.AddMemberAsync(_factory, owner.OrganizationId, SystemRoleSeed.SalesRepRoleId, token);
        using var repClient = CrmApi.Client(_factory, rep.AccessToken);

        var response = await DealApi.MoveAsync(repClient, DealApi.IdOf(deal), PipelineApi.Named(stages, QualifiedStageName).Id, 0, DealApi.VersionOf(deal), token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await CrmApi.ErrorCodeAsync(response, token)).Should().Be(CrmErrorCodes.DealNotFound);
    }

    [Fact]
    public async Task RepeatedInsertsIntoOneGap_RenumberTheStage_AndKeepTheBoardOrder()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var stages = await PipelineApi.StagesAsync(admin, await PipelineApi.DefaultPipelineIdAsync(admin, token), token);
        var newStageId = PipelineApi.Named(stages, NewStageName).Id;
        var deals = new List<Guid>();
        for (var index = 0; index < CrowdedDealCount; index++)
        {
            deals.Add(DealApi.IdOf(await DealApi.CreateAsync(admin, $"Crowded {index:D2}", 10m + index, newStageId, null, token)));
        }

        // Each deal from the third one on moves to the second place: the gap after the first deal halves every time.
        for (var index = FirstMovedIndex; index < CrowdedDealCount; index++)
        {
            var current = await DealApi.ReadAsync(admin, deals[index], token);
            var moved = await DealApi.MoveAsync(admin, deals[index], newStageId, 1, DealApi.VersionOf(current), token);
            moved.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var expected = new List<Guid> { deals[0] };
        expected.AddRange(Enumerable.Range(FirstMovedIndex, CrowdedDealCount - FirstMovedIndex).Reverse().Select(index => deals[index]));
        expected.Add(deals[1]);
        (await DealApi.ListIdsAsync(admin, $"stageId={newStageId:D}&sort=position&limit=100", token)).Should().Equal(expected);
    }

    private async Task<int> CountHistoryAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            return await db.DealStageHistory.CountAsync(entry => entry.DealId == dealId, cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
    }

    private async Task<List<string>> ReadActivityTypesAsync(Guid organizationId, Guid dealId, CancellationToken cancellationToken)
    {
        using var scope = _factory.Services.CreateScope();
        var tenant = scope.ServiceProvider.GetRequiredService<IMutableTenantContext>();
        var db = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        tenant.Set(organizationId);
        try
        {
            return await db.Activities
                .Where(activity => activity.DealId == dealId)
                .Select(activity => activity.Type)
                .ToListAsync(cancellationToken);
        }
        finally
        {
            tenant.Clear();
        }
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
}
