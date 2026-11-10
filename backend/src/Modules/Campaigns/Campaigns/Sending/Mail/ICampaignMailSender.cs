namespace Parley.Campaigns.Sending.Mail;

/// <summary>
/// Submits one campaign email to the mail server. The implementation reports the outcome instead of throwing for
/// mail failures, so the caller can tell a safe retry from a result that is unknown. Cancellation is the only exception
/// it lets through. It must never log the address or the body.
/// </summary>
public interface ICampaignMailSender
{
    Task<CampaignMailOutcome> SendAsync(CampaignMailMessage message, CancellationToken cancellationToken);
}
