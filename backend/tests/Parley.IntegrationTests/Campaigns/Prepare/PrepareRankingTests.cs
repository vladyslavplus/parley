using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Insights.Contracts.Scores;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Prepare;

/// <summary>
/// Ranking prepare (domain model 7.4): the score ranks the whole audience once, across both arms. The top share is
/// targeted in each arm, ranks run 1..n with no gaps, and every row, in either arm, carries the score it was ranked on.
/// </summary>
public sealed class PrepareRankingTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    /// <summary>Sixteen contacts: with a half holdout, the chance that every contact lands in one arm is about 3 in 100 000.</summary>
    private const int ContactCount = 16;

    private const int TargetingShareBps = 5000;

    private const int HoldoutShareBps = 5000;

    /// <summary>Both arms are asserted to be present, so the split is pinned to have both before the freeze.</summary>
    private const int MinimumHoldout = 1;

    private const int MinimumTreatment = 1;

    private const string RankedContactPrefix = "ranked";

    private IdentityApiFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraServices: services =>
            {
                services.RemoveAll<IScoreReader>();
                services.AddSingleton<IScoreReader, StubScoreReader>();
            });
    }

    public ValueTask DisposeAsync()
    {
        _factory.Dispose();
        return ValueTask.CompletedTask;
    }

    [Fact]
    public async Task Prepare_TopUplift_RanksOnceAcrossBothArms_AndStoresTheScoreOfEveryRow()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"{RankedContactPrefix}{index}"), token);
        }

        var campaignId = await CampaignSendHarness.CreateCampaignAsync(
            admin,
            templateId,
            HoldoutShareBps,
            CampaignTargetingModes.TopUplift,
            TargetingShareBps,
            token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, MinimumHoldout, MinimumTreatment);

        var rows = await ReadRowsAsync(campaignId, token);
        var targetCount = ContactCount / 2; // a 50 percent targeting share, as TargetingShareBps states
        rows.Should().HaveCount(ContactCount);
        rows.Select(row => row.Arm).Distinct().Should().HaveCount(2, "both arms are present, so the check covers both");

        // Every row, in either arm, carries the score it was ranked on.
        rows.Should().OnlyContain(row => row.ScoreKind == ScoreKinds.Uplift && row.ScoreValue != null && row.ModelVersion != null);

        // Ranks are 1..n with no gaps and no repeats, whatever the arm.
        rows.Select(row => row.Rank).Should().BeEquivalentTo(Enumerable.Range(1, ContactCount).Select(rank => (int?)rank));

        // The top share of the whole audience is targeted, in both arms alike.
        rows.Where(row => row.IsTargeted).Should().HaveCount(targetCount);
        rows.Should().OnlyContain(row => row.IsTargeted == (row.Rank <= targetCount));
        var targetedLowestScore = rows.Where(row => row.IsTargeted).Min(row => row.ScoreValue!.Value);
        var untargetedHighestScore = rows.Where(row => !row.IsTargeted).Max(row => row.ScoreValue!.Value);
        targetedLowestScore.Should().BeGreaterThanOrEqualTo(untargetedHighestScore, "the targeted rows are the top scores");
    }

    private async Task<List<RecipientRow>> ReadRowsAsync(Guid campaignId, CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            """
            SELECT arm, is_targeted, rank_in_audience, score_kind, score_value, model_version
            FROM campaigns.recipients WHERE campaign_id = @campaign
            """,
            connection);
        command.Parameters.AddWithValue("campaign", campaignId);
        await using var reader = await command.ExecuteReaderAsync(token);
        var rows = new List<RecipientRow>();
        while (await reader.ReadAsync(token))
        {
            rows.Add(new RecipientRow(
                reader.GetString(0),
                reader.GetBoolean(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        return rows;
    }

    private sealed record RecipientRow(string Arm, bool IsTargeted, int? Rank, string? ScoreKind, decimal? ScoreValue, string? ModelVersion);
}
