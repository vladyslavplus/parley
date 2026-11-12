using AwesomeAssertions;
using Parley.Insights.Domain.Priority;
using Xunit;

namespace Parley.UnitTests.Insights;

/// <summary>Per-owner capacity, ranks without gaps, the unowned group, and the tie rule on equal values.</summary>
public sealed class PriorityBucketsTests
{
    private static readonly Guid OwnerA = Id(900);

    private static readonly Guid OwnerB = Id(901);

    [Fact]
    public void Ranked_KeepsTheBestCapacityPerOwner_WithRanksFromOne()
    {
        var buckets = new PriorityBuckets(2);
        buckets.Add(Candidate(1, OwnerA, 10m));
        buckets.Add(Candidate(2, OwnerA, 30m));
        buckets.Add(Candidate(3, OwnerA, 20m));
        buckets.Add(Candidate(4, OwnerB, 5m));

        var ranked = buckets.Ranked();

        ranked.Where(entry => entry.Candidate.OwnerUserId == OwnerA)
            .Select(entry => (entry.Candidate.ContactId, entry.Rank))
            .Should().Equal((Id(2), 1), (Id(3), 2));
        ranked.Where(entry => entry.Candidate.OwnerUserId == OwnerB)
            .Select(entry => entry.Rank)
            .Should().Equal(1);
    }

    [Fact]
    public void Ranked_KeepsUnownedContactsAsTheirOwnGroup()
    {
        var buckets = new PriorityBuckets(1);
        buckets.Add(Candidate(1, null, 10m));
        buckets.Add(Candidate(2, null, 40m));
        buckets.Add(Candidate(3, OwnerA, 7m));

        var ranked = buckets.Ranked();

        ranked.Where(entry => entry.Candidate.OwnerUserId is null)
            .Select(entry => entry.Candidate.ContactId)
            .Should().Equal(Id(2));
        ranked.Should().HaveCount(2);
    }

    [Fact]
    public void Ranked_ResolvesEqualValuesToTheLowerContactId()
    {
        var buckets = new PriorityBuckets(1);
        buckets.Add(Candidate(9, OwnerA, 10m));
        buckets.Add(Candidate(4, OwnerA, 10m));

        buckets.Ranked().Single().Candidate.ContactId.Should().Be(Id(4));
    }

    private static PriorityCandidate Candidate(int index, Guid? owner, decimal value) => new(
        Id(index),
        owner,
        "regular",
        value,
        [],
        PrioritySuggestedActions.Nurture);

    private static Guid Id(int index) => new($"00000000-0000-0000-0000-{index:D12}");
}
