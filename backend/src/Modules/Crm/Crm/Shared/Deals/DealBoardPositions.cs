using Parley.Crm.Domain.Deals;

namespace Parley.Crm.Shared.Deals;

/// <summary>
/// Board position arithmetic. Deals of one stage sit on gaps of <see cref="CrmLimits.DealBoardPositionGap"/>, so
/// an insert between two neighbours takes the middle value and only a collision (no room left) renumbers the stage.
/// </summary>
public static class DealBoardPositions
{
    /// <summary>
    /// Position for a deal placed between two neighbours, where either side may be absent. Returns null when the
    /// neighbours leave no free value between them; the caller then renumbers the whole stage.
    /// </summary>
    public static long? Between(long? previous, long? next) => (previous, next) switch
    {
        (null, null) => CrmLimits.DealBoardPositionGap,
        (null, { } after) => after - CrmLimits.DealBoardPositionGap,
        ({ } before, null) => before + CrmLimits.DealBoardPositionGap,
        ({ } before, { } after) when after - before >= 2 => before + ((after - before) / 2),
        _ => null,
    };

    /// <summary>Gives the deals, in the given order, evenly spaced positions. Used on collision only.</summary>
    public static void Renumber(IReadOnlyList<Deal> orderedDeals)
    {
        for (var index = 0; index < orderedDeals.Count; index++)
        {
            orderedDeals[index].BoardPosition = (index + 1) * CrmLimits.DealBoardPositionGap;
        }
    }
}
