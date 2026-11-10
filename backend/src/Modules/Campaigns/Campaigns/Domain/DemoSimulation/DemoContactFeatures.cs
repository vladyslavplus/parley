namespace Parley.Campaigns.Domain.DemoSimulation;

/// <summary>
/// The eight feature-schema fields (version 1) of one contact as of an instant. A field without purchases is NaN, so no
/// segment rule that compares it matches, the same as the reference module.
/// </summary>
public sealed record DemoContactFeatures(
    double RecencyDays,
    double TenureDays,
    double Orders90Days,
    double Orders365Days,
    double OrderCount,
    double Value365Days,
    double AvgOrderValue,
    double HasOpenDeal);
