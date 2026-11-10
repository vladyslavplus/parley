using System.Net;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Contracts.Events;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Sending.Mail;
using Parley.Crm.Domain.Activities;
using Parley.Insights.Contracts.Scores;
using Parley.IntegrationTests.Campaigns.Send.Support;
using Parley.IntegrationTests.Crm.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Xunit;

namespace Parley.IntegrationTests.Campaigns.Send;

/// <summary>
/// The unsubscribe of a campaign email: the body links to the web page, the page's POST works without a sign-in, a GET
/// changes nothing, a repeat changes nothing, a forged token is refused with a stable code, and the suppression reaches
/// the timeline.
/// </summary>
public sealed class UnsubscribeTests(PostgresFixture postgres, MailpitFixture mailpit) : IAsyncLifetime
{
    private const string TokenParameter = "token=";

    private const string UnsubscribePath = "/unsubscribe?" + TokenParameter;

    private const string ProblemCodeProperty = "code";

    private const int HoldoutShareBps = 1000;

    private const int ContactCount = 8;

    /// <summary>The test sends at least one email, so the split is pinned to have a treatment contact (see the harness).</summary>
    private const int MinimumTreatment = 1;

    private const string Culture = "en";

    private RecordingCampaignMailSender _sender = null!;

    private IdentityApiFactory _factory = null!;

    private string _connectionString = null!;

    public async ValueTask InitializeAsync()
    {
        await postgres.ResetDataAsync(TestContext.Current.CancellationToken);
        _connectionString = postgres.ConnectionString;
        _sender = new RecordingCampaignMailSender();
        var sender = _sender;
        _factory = new IdentityApiFactory(
            postgres,
            mailpit,
            extraServices: services =>
            {
                services.RemoveAll<ICampaignMailSender>();
                services.AddSingleton<ICampaignMailSender>(sender);
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
    public async Task UnsubscribeLink_IsPostOnly_IsRepeatable_RefusesForgedTokens_AndReachesTheTimeline()
    {
        var token = TestContext.Current.CancellationToken;
        var owner = await IdentitySetup.CreateOwnerAsync(_factory, mailpit, token);
        using var admin = CrmApi.Client(_factory, owner.AccessToken);
        var templateId = await CampaignSendHarness.CreateTemplateAsync(admin, token);
        // Several contacts: a holdout share can keep one contact out of the email, so the test never rests on a single draw.
        for (var index = 0; index < ContactCount; index++)
        {
            await CrmApi.CreateContactAsync(admin, CampaignSendHarness.ContactAt($"leaving{index}"), token);
        }
        var campaignId = await CampaignSendHarness.CreateCampaignAsync(admin, templateId, HoldoutShareBps, CampaignTargetingModes.All, null, token);
        await CampaignSendHarness.PrepareAsync(_factory, admin, owner.OrganizationId, campaignId, token, minimumTreatment: MinimumTreatment);
        var started = await CampaignSendHarness.StartAsync(admin, campaignId, "unsubscribe-start", token);
        started.StatusCode.Should().Be(HttpStatusCode.Accepted);
        await CampaignSendHarness.RunSendJobAsync(_factory, owner.OrganizationId, campaignId, token);

        _sender.Calls.Should().NotBeEmpty("at least one contact is outside the holdout");
        var email = _sender.Calls[0];
        var linkToken = TokenOf(email.OneClickUnsubscribeUrl);
        var address = email.ToAddress.Trim().ToLowerInvariant();

        // The body links to the web page with the culture in its path. The page, not the API, is what the recipient opens.
        var pageLink = TestHostSettings.PublicBaseUrl + "/" + Culture + "/unsubscribe?" + TokenParameter + Uri.EscapeDataString(linkToken);
        email.HtmlBody.Should().Contain(pageLink);
        email.PlainTextBody.Should().Contain(pageLink);

        using var anonymous = _factory.CreateClient();

        // A mail scanner that prefetches the link sends a GET. Nothing changes.
        var prefetch = await anonymous.GetAsync(PathWith(linkToken), token);
        prefetch.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        (await SuppressionCountAsync(address, token)).Should().Be(0);

        // The page's confirmation button, or a mail client's one-click POST, needs no session.
        var first = await anonymous.PostAsync(PathWith(linkToken), null, token);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SuppressionCountAsync(address, token)).Should().Be(1);

        // A repeat changes nothing: no second suppression, no second event.
        var repeat = await anonymous.PostAsync(PathWith(linkToken), null, token);
        repeat.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await SuppressionCountAsync(address, token)).Should().Be(1);
        (await CampaignSendHarness.CountAsync(_connectionString, "campaigns", "outbox_messages", "type", typeof(RecipientUnsubscribed).FullName!, token))
            .Should().Be(1);

        // A token with its last character changed is a forgery.
        var forged = linkToken[..^1] + (linkToken[^1] == 'A' ? 'B' : 'A');
        var forgedResponse = await anonymous.PostAsync(PathWith(forged), null, token);
        forgedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthTestClient.ReadJsonAsync(forgedResponse, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.UnsubscribeTokenInvalid);

        var missing = await anonymous.PostAsync("/unsubscribe", null, token);
        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AuthTestClient.ReadJsonAsync(missing, token)).GetProperty(ProblemCodeProperty).GetString()
            .Should().Be(CampaignsErrorCodes.UnsubscribeTokenInvalid);

        await CampaignSendHarness.DeliverCampaignEventsAsync(_factory, token);
        (await CampaignSendHarness.CountAsync(_connectionString, "crm", "activities", "type", ActivityTypes.Unsubscribed, token))
            .Should().Be(1, "the unsubscribe is on the contact's timeline");
    }

    private static string TokenOf(string url)
    {
        var start = url.IndexOf(TokenParameter, StringComparison.Ordinal) + TokenParameter.Length;
        return Uri.UnescapeDataString(url[start..]);
    }

    private static string PathWith(string token) => UnsubscribePath + Uri.EscapeDataString(token);

    private async Task<long> SuppressionCountAsync(string address, CancellationToken cancellationToken) =>
        await CampaignSendHarness.CountAsync(_connectionString, "campaigns", "suppressions", "email_normalized", address, cancellationToken);
}
