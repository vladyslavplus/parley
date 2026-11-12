namespace Parley.Insights.Domain.Priority;

/// <summary>
/// Inputs of the ML-mode rule, taken from the priority options: the weights of the uplift and the RFM terms, the segment
/// weights (the largest one normalizes them), the uplift mark that earns a reason, and the CRM bonuses.
/// </summary>
public sealed record PriorityMlOptions(
    decimal UpliftWeight,
    decimal RfmWeight,
    decimal UpliftHighThreshold,
    IReadOnlyDictionary<string, decimal> Weights,
    decimal TaskDueBonus,
    decimal StaleDealBonus);
