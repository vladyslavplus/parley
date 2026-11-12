namespace Parley.Insights.Domain.Priority;

/// <summary>
/// Keeps the best <c>capacityPerOwner</c> candidates of each owner while the candidates stream by, so memory grows with the
/// owners and the capacity, not with the contacts. Each owner has a min-heap whose top is the worst kept candidate; a
/// candidate replaces that top only when it ranks higher. Unowned candidates form one group of their own. Ties on value
/// rank the lower contact id first.
/// </summary>
public sealed class PriorityBuckets(int capacityPerOwner)
{
    private static readonly IComparer<PriorityCandidate> WorstFirst = Comparer<PriorityCandidate>.Create(CompareWorstFirst);

    private readonly Dictionary<Guid, PriorityQueue<PriorityCandidate, PriorityCandidate>> _owners = [];

    private readonly PriorityQueue<PriorityCandidate, PriorityCandidate> _unowned = new(WorstFirst);

    public void Add(PriorityCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var heap = HeapFor(candidate.OwnerUserId);
        if (heap.Count < capacityPerOwner)
        {
            heap.Enqueue(candidate, candidate);
            return;
        }

        if (WorstFirst.Compare(heap.Peek(), candidate) < 0)
        {
            heap.Dequeue();
            heap.Enqueue(candidate, candidate);
        }
    }

    /// <summary>The kept candidates of every group, best first within each group, ranks from 1 without gaps.</summary>
    public IReadOnlyList<RankedPriorityCandidate> Ranked()
    {
        var ranked = new List<RankedPriorityCandidate>();
        foreach (var heap in _owners.Values.Append(_unowned))
        {
            var rank = 1;
            foreach (var candidate in heap.UnorderedItems.Select(item => item.Element).OrderByDescending(item => item, WorstFirst))
            {
                ranked.Add(new RankedPriorityCandidate(candidate, rank));
                rank++;
            }
        }

        return ranked;
    }

    private PriorityQueue<PriorityCandidate, PriorityCandidate> HeapFor(Guid? ownerUserId)
    {
        if (ownerUserId is not { } owner)
        {
            return _unowned;
        }

        if (!_owners.TryGetValue(owner, out var heap))
        {
            heap = new PriorityQueue<PriorityCandidate, PriorityCandidate>(WorstFirst);
            _owners[owner] = heap;
        }

        return heap;
    }

    /// <summary>Negative when <paramref name="left"/> is the worse candidate: lower value, or the same value with the higher contact id.</summary>
    private static int CompareWorstFirst(PriorityCandidate left, PriorityCandidate right)
    {
        var byValue = left.Value.CompareTo(right.Value);
        return byValue != 0 ? byValue : right.ContactId.CompareTo(left.ContactId);
    }
}
