using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Campaigns;
using Parley.Campaigns.Features.Campaigns.Dispatch.Claims;
using Parley.Campaigns.Features.Campaigns.Dispatch.Outcomes;
using Parley.Campaigns.Features.Campaigns.Dispatch.Pacing;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Sending.Mail;
using Parley.Campaigns.Sending.Messages;
using Parley.Campaigns.Sending.Unsubscribe;
using Parley.Campaigns.Templates.Model;
using Parley.Crm.Contracts.Recipients;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.Dispatch.Job;

/// <summary>
/// Sends a sending campaign batch by batch until nothing is claimable, then tries to complete it. Each batch is claimed
/// under a lease first, then rendered and sent outside any transaction, and each outcome is recorded on its own. A
/// send whose outcome cannot be known is never repeated.
/// </summary>
public sealed class CampaignSendRunner(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    CampaignSendClaimer claimer,
    CampaignSendRecorder recorder,
    CampaignSendCompletion completion,
    CampaignSendPacer pacer,
    CampaignMessageRenderer renderer,
    ICampaignMailSender smtpMailSender,
    DemoCampaignMailSender demoMailSender,
    ICrmRecipientProfileReader profiles,
    UnsubscribeTokenCodec tokens,
    IOptions<CampaignsSendOptions> options,
    ILogger<CampaignSendRunner> logger)
{
    private readonly CampaignsSendOptions _options = options.Value;

    public async Task RunAsync(Guid campaignId, CancellationToken cancellationToken)
    {
        var campaign = await dbContext.Campaigns
            .AsNoTracking()
            .Where(row => row.Id == campaignId)
            .Select(row => new { row.Status, row.TemplateId, row.Source })
            .FirstOrDefaultAsync(cancellationToken);
        if (campaign is null || campaign.Status != CampaignStatuses.Sending)
        {
            return;
        }

        // Historic demo campaigns are submitted to the demo sender, never to the mail server. The rest of the send is the same.
        var demo = campaign.Source == CampaignSources.DemoSeed;

        var template = await dbContext.Templates
            .AsNoTracking()
            .Where(row => row.Id == campaign.TemplateId)
            .Select(row => new { row.SubjectLiquid, row.BodyLiquid })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("The campaign template no longer exists.");
        var compiled = renderer.Compile(template.SubjectLiquid, template.BodyLiquid);
        if (!compiled.IsSuccess)
        {
            throw new InvalidOperationException("A saved campaign template no longer compiles.");
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await claimer.ExpireLeasesAsync(campaignId, cancellationToken);
            var batch = await claimer.ClaimBatchAsync(campaignId, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            var profileRows = await profiles.ReadBatchAsync(batch.Select(send => send.ContactId).Distinct().ToList(), cancellationToken);
            var profilesByContact = profileRows.ToDictionary(row => row.ContactId);
            foreach (var send in batch)
            {
                await SendOneAsync(campaignId, send, profilesByContact, compiled.Value!, demo, cancellationToken);
            }
        }

        await completion.TryCompleteAsync(campaignId, cancellationToken);
    }

    private async Task SendOneAsync(
        Guid campaignId,
        ClaimedSend send,
        IReadOnlyDictionary<Guid, CrmRecipientProfileRow> profilesByContact,
        CompiledCampaignTemplate template,
        bool demo,
        CancellationToken cancellationToken)
    {
        // Recorded outcomes run under CancellationToken.None: a shutdown must not drop a sent message's record.
        if (!profilesByContact.TryGetValue(send.ContactId, out var profile)
            || string.IsNullOrEmpty(send.Address)
            || !MailboxAddress.TryParse(send.Address, out _))
        {
            await recorder.MarkFailedAsync(send, CampaignsErrorCodes.SendRecipientUnavailable, CancellationToken.None);
            return;
        }

        var token = tokens.Issue(tenantContext.OrganizationId, campaignId, send.ContactId);
        var pageUrl = UnsubscribeLinks.BuildPage(_options.PublicWebBaseUrl, send.Locale, token);
        var oneClickUrl = UnsubscribeLinks.BuildOneClick(_options.PublicApiBaseUrl, token);
        var model = new TemplateModel(
            new ContactTemplateModel(profile.FirstName, profile.LastName),
            new CompanyTemplateModel(profile.CompanyName),
            pageUrl);
        var rendered = await renderer.RenderAsync(template, model, cancellationToken);
        if (!rendered.IsSuccess)
        {
            await recorder.MarkFailedAsync(send, rendered.Error!.Code, CancellationToken.None);
            return;
        }

        // The pacer limits the mail server only. The demo sender opens no connection, so its sends are not paced.
        if (!demo)
        {
            await pacer.WaitTurnAsync(cancellationToken);
        }

        // The lease is renewed right before submission. The batch may have waited long on the pacer or the server; a claim
        // whose lease already ended is left for the expiry sweep and is never submitted.
        if (!await claimer.RenewLeaseAsync(send.SendId, cancellationToken))
        {
            return;
        }

        var message = new CampaignMailMessage(
            send.MessageId,
            send.Address,
            rendered.Value!.Subject,
            rendered.Value.HtmlBody,
            rendered.Value.PlainTextBody,
            oneClickUrl,
            send.Locale);

        CampaignMailOutcome outcome;
        try
        {
            outcome = demo
                ? await demoMailSender.SendAsync(message, cancellationToken)
                : await smtpMailSender.SendAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The claim stays claimed. Its lease ends, and the sweep turns it into unknown; it is never sent again.
            throw;
        }
        catch (Exception exception)
        {
            // Only the exception type is logged: its message may carry the address or the server name.
            logger.LogWarning("Mail submission for claim {SendId} failed with {ExceptionType}; the outcome is unknown.", send.SendId, exception.GetType().Name);
            outcome = CampaignMailOutcome.Unknown;
        }

        switch (outcome)
        {
            case CampaignMailOutcome.Accepted:
                await recorder.MarkSentAsync(campaignId, send, CancellationToken.None);
                break;
            case CampaignMailOutcome.NotAccepted:
                await recorder.MarkNotAcceptedAsync(send, _options.MaxAttempts, CampaignsErrorCodes.SendMailUnavailable, CancellationToken.None);
                break;
            default:
                await recorder.MarkUnknownAsync(send, CampaignsErrorCodes.SendResultUnknown, CancellationToken.None);
                break;
        }
    }
}
