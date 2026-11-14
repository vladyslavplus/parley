using Parley.Campaigns.Domain.DemoSimulation;

namespace Parley.Campaigns.Features.Campaigns.DemoSeed;

/// <summary>
/// The effect the injected demo campaign applies to one contact: the documented effect of the demo (the segments of
/// <c>ml/configs/crm_uplift_demo.yaml</c>) times the campaign multiplier of <see cref="CampaignDemoSpecs"/>. The
/// multiplier is a property of the campaign send, not of the effect function, so the ML fixture keeps the documented
/// effect.
/// </summary>
public static class CampaignDemoEffect
{
    /// <summary>The conversion gain of the contact under the injected demo campaign.</summary>
    public static double Tau(DemoContactFeatures features) =>
        DemoEffect.Tau(features) * CampaignDemoSpecs.InjectedEffectMultiplier;
}
