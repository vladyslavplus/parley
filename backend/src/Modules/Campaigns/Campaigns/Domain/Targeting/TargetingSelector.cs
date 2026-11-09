using Parley.Campaigns.Shared;

namespace Parley.Campaigns.Domain.Targeting;

/// <summary>
/// Score ranking of a campaign audience (CMP-08). The whole eligible audience is ranked across both arms, so the same
/// contacts are targeted in either arm and the arm-specific behavior comes after this step (domain model 7.4).
/// Ties break on the contact id, so the order does not depend on the order the audience was read in.
/// </summary>
public static class TargetingSelector
{
    /// <summary>Contacts ranked highest first. Only contacts that have a score are ranked.</summary>
    public static IReadOnlyList<Guid> Order(IReadOnlyList<(Guid ContactId, decimal Score)> scored) =>
        scored
            .OrderByDescending(entry => entry.Score)
            .ThenBy(entry => entry.ContactId)
            .Select(entry => entry.ContactId)
            .ToArray();

    /// <summary>How many contacts the share targets, rounded down, from the size of the whole eligible audience.</summary>
    public static int TargetCount(int audienceSize, int shareBps) =>
        (int)((long)audienceSize * shareBps / BasisPoints.Scale);
}
