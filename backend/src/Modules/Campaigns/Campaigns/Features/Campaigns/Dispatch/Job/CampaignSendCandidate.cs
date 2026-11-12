namespace Parley.Campaigns.Features.Campaigns.Dispatch.Job;

/// <summary>Unmapped row of the recovery sweep: a sending campaign that needs a runner, with the organization it runs under.</summary>
public sealed class CampaignSendCandidate
{
    public Guid OrganizationId { get; set; }

    public Guid CampaignId { get; set; }
}
