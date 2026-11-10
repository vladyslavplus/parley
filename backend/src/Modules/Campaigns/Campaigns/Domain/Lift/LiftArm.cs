namespace Parley.Campaigns.Domain.Lift;

/// <summary>
/// One arm of a two-arm comparison. <paramref name="Count"/> is the number of recipients the comparison counts,
/// <paramref name="Conversions"/> how many of them converted, and <paramref name="RevenueSum"/> the total revenue of all of
/// them (zero for those who bought nothing). A mean is defined only when the count is positive.
/// </summary>
public sealed record LiftArm(long Count, long Conversions, double RevenueSum);
