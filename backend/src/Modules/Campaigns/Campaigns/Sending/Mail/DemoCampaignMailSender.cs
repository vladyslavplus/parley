namespace Parley.Campaigns.Sending.Mail;

/// <summary>
/// Submission of the historic demo campaigns. It reports every message as accepted and opens no connection to a mail
/// server, so a demo seed never sends mail. The send runner picks it only for campaigns whose source is the demo seed; the
/// claim, render and mark steps are the same as for a real send.
/// </summary>
public sealed class DemoCampaignMailSender : ICampaignMailSender
{
    public Task<CampaignMailOutcome> SendAsync(CampaignMailMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(CampaignMailOutcome.Accepted);
    }
}
