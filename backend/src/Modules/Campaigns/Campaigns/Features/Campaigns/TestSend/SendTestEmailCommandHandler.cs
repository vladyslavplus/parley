using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Campaigns.Contracts.Errors;
using Parley.Campaigns.Domain.Sends;
using Parley.Campaigns.Options;
using Parley.Campaigns.Persistence;
using Parley.Campaigns.Sending.Mail;
using Parley.Campaigns.Sending.Messages;
using Parley.Campaigns.Sending.Unsubscribe;
using Parley.Campaigns.Templates.Model;
using Parley.Identity.Contracts.Users;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.Campaigns.Features.Campaigns.TestSend;

public sealed class SendTestEmailCommandHandler(
    CampaignsDbContext dbContext,
    ITenantContext tenantContext,
    ICurrentUser currentUser,
    IUserDirectory users,
    CampaignMessageRenderer renderer,
    ICampaignMailSender mailSender,
    CampaignTestSendLimiter testSendLimiter,
    IOptions<CampaignsSendOptions> options) : IRequestHandler<SendTestEmailCommand, Result>
{
    public async ValueTask<Result> Handle(SendTestEmailCommand command, CancellationToken cancellationToken)
    {
        var campaignExists = await dbContext.Campaigns
            .AsNoTracking()
            .AnyAsync(campaign => campaign.Id == command.CampaignId, cancellationToken);
        if (!campaignExists)
        {
            return Result.Failure(Error.NotFound(CampaignsErrorCodes.CampaignNotFound));
        }

        // Archived templates still send: a campaign keeps the template it was started with.
        var template = await dbContext.Campaigns
            .AsNoTracking()
            .Where(campaign => campaign.Id == command.CampaignId)
            .Join(
                dbContext.Templates.AsNoTracking(),
                campaign => campaign.TemplateId,
                template => template.Id,
                (_, template) => new { template.SubjectLiquid, template.BodyLiquid, template.Locale })
            .FirstOrDefaultAsync(cancellationToken);
        if (template is null)
        {
            return Result.Failure(Error.NotFound(CampaignsErrorCodes.TemplateNotFound));
        }

        var compiled = renderer.Compile(template.SubjectLiquid, template.BodyLiquid);
        if (!compiled.IsSuccess)
        {
            return Result.Failure(compiled.Error!);
        }

        var userId = currentUser.RequireUserId();
        if (!testSendLimiter.TryAcquire(userId))
        {
            return Result.Failure(Error.RateLimited(CampaignsErrorCodes.TestSendRateLimited));
        }

        var entries = await users.FindMembersAsync(tenantContext.OrganizationId, [userId], cancellationToken);
        if (!entries.TryGetValue(userId, out var entry) || entry.Email is null)
        {
            return Result.Failure(Error.Validation(CampaignsErrorCodes.SendTestAddressMissing));
        }

        // Sample values stand in for a recipient. The links carry the sample token, which no recipient is ever issued.
        var settings = options.Value;
        var pageUrl = UnsubscribeLinks.BuildPage(settings.PublicWebBaseUrl, template.Locale, TemplateSampleData.UnsubscribeToken);
        var oneClickUrl = UnsubscribeLinks.BuildOneClick(settings.PublicApiBaseUrl, TemplateSampleData.UnsubscribeToken);
        var rendered = await renderer.RenderAsync(compiled.Value!, TemplateSampleData.WithUnsubscribeUrl(pageUrl), cancellationToken);
        if (!rendered.IsSuccess)
        {
            return Result.Failure(rendered.Error!);
        }

        var message = new CampaignMailMessage(
            CampaignMessageIds.ForTest(command.CampaignId, userId, Guid.CreateVersion7(), settings.MessageIdDomain),
            entry.Email,
            rendered.Value!.Subject,
            rendered.Value.HtmlBody,
            rendered.Value.PlainTextBody,
            oneClickUrl,
            template.Locale);

        CampaignMailOutcome outcome;
        try
        {
            outcome = await mailSender.SendAsync(message, cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // A test has no claim to hold, so a failure only has to be reported; the caller may try again.
            outcome = CampaignMailOutcome.Unknown;
        }

        return outcome == CampaignMailOutcome.Accepted
            ? Result.Success()
            : Result.Failure(Error.Failure(CampaignsErrorCodes.SendTestFailed));
    }
}
