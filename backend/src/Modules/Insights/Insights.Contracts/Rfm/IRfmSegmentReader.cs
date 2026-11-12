namespace Parley.Insights.Contracts.Rfm;

/// <summary>
/// Narrow read of RFM segments of the current organization, from the latest <em>complete</em> snapshot day. The contacts
/// module uses it to show a contact's segment in the list and to filter the list by segment; it reads nothing else of the
/// insights data. Without a complete day, every read is empty.
/// </summary>
public interface IRfmSegmentReader
{
    /// <summary>The segment of each listed contact that has a snapshot row on the latest complete day. Contacts without one are absent.</summary>
    Task<IReadOnlyDictionary<Guid, string>> ReadSegmentsAsync(IReadOnlyCollection<Guid> contactIds, CancellationToken cancellationToken);

    /// <summary>
    /// The contacts of one segment on the latest complete day, at most <paramref name="maximum"/> of them. When
    /// <paramref name="visibleOwnerUserIds"/> is set, only contacts owned by those users count, so the bound applies to what
    /// the caller can see; null counts every contact of the organization. When more exist,
    /// <see cref="RfmSegmentMembers.Truncated"/> is true and the caller must not treat the list as complete.
    /// </summary>
    Task<RfmSegmentMembers> ReadMemberIdsAsync(
        string segment,
        int maximum,
        IReadOnlyCollection<Guid>? visibleOwnerUserIds,
        CancellationToken cancellationToken);
}
