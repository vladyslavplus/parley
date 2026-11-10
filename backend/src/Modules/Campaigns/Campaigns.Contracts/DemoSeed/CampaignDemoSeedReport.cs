using Parley.Campaigns.Contracts.Results;

namespace Parley.Campaigns.Contracts.DemoSeed;

/// <summary>Results of the historic campaigns and the figures of the injected effect.</summary>
/// <param name="AaResults">Result of the A/A campaign. No effect was injected, so its interval should cover zero.</param>
/// <param name="InjectedResults">Result of the injected-effect campaign, with its simulated orders labeled.</param>
/// <param name="TreatedTargetedContacts">Treatment-arm targeted contacts of the injected campaign.</param>
/// <param name="SimulatedOrdersWritten">Simulated orders this call created; 0 on a re-run.</param>
/// <param name="NegativeRemovalsSkipped">Buyers whose negative configured effect would have removed an order. Real orders are never removed, so none is written.</param>
/// <param name="ConfiguredMeanEffect">Mean configured effect (tau) over the treatment-arm targeted contacts, before any draw.</param>
/// <param name="ExpectedWrittenLift">Expected conversion lift that the positive effect adds per treatment-arm contact, before any draw.</param>
public sealed record CampaignDemoSeedReport(
    CampaignResultsSnapshot AaResults,
    CampaignResultsSnapshot InjectedResults,
    int TreatedTargetedContacts,
    int SimulatedOrdersWritten,
    int NegativeRemovalsSkipped,
    double ConfiguredMeanEffect,
    double ExpectedWrittenLift);
