namespace Parley.Insights.Domain.Rfm;

/// <summary>
/// Quintile tiers with the semantics of SQL <c>ntile(5)</c>: n rows fill five buckets, and the first n mod 5 buckets hold one
/// extra row. Rows are ordered by their value, and rows with equal values are ordered by contact id, so the tiers do not
/// depend on the order the rows were read in. Equal values can therefore land in different tiers; that is the tie rule.
/// With fewer than five contacts every contact has its own tier, from 1 upward.
/// The tiers are relative to the organization's own customers: a segment says where a contact stands among that organization's
/// buyers, not against a fixed scale. With fewer than five buyers the tiers are coarse, because each buyer fills a bucket alone.
/// The tie rule is deliberate: it is the plan's ntile with a contact-id tie-break, and it is not changed to split equal values.
/// </summary>
public static class RfmTierCalculator
{
    public const int Buckets = 5;

    /// <summary>The bucket, 1 to <paramref name="buckets"/>, of the row at <paramref name="index"/> (0-based) among <paramref name="count"/> rows.</summary>
    public static int Ntile(int index, int count, int buckets)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(buckets);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);

        var size = count / buckets;
        var extra = count % buckets;
        var largeRows = (size + 1) * extra;
        return index < largeRows
            ? index / (size + 1) + 1
            : extra + (index - largeRows) / size + 1;
    }

    /// <summary>
    /// The tiers of each contact. Recency is ranked the other way round: the longest gap gets tier 1, so the most recent
    /// contact gets tier 5. Frequency and value rank upward, so the most orders and the highest value get tier 5.
    /// </summary>
    public static IReadOnlyDictionary<Guid, RfmTiers> Compute(IReadOnlyList<RfmInput> contacts)
    {
        ArgumentNullException.ThrowIfNull(contacts);
        var recency = Rank(contacts, contact => contact.RecencyDays, descending: true);
        var frequency = Rank(contacts, contact => contact.Orders365Days, descending: false);
        var monetary = Rank(contacts, contact => contact.Value365Days, descending: false);

        return contacts.ToDictionary(
            contact => contact.ContactId,
            contact => new RfmTiers(recency[contact.ContactId], frequency[contact.ContactId], monetary[contact.ContactId]));
    }

    private static Dictionary<Guid, int> Rank<TKey>(IReadOnlyList<RfmInput> contacts, Func<RfmInput, TKey> key, bool descending)
        where TKey : IComparable<TKey>
    {
        var ordered = descending
            ? contacts.OrderByDescending(key).ThenBy(contact => contact.ContactId)
            : contacts.OrderBy(key).ThenBy(contact => contact.ContactId);

        var tiers = new Dictionary<Guid, int>(contacts.Count);
        var index = 0;
        foreach (var contact in ordered)
        {
            tiers[contact.ContactId] = Ntile(index, contacts.Count, Buckets);
            index++;
        }

        return tiers;
    }
}
