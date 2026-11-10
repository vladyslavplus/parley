using AwesomeAssertions;
using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Domain.Recipients;
using Parley.Campaigns.Shared;
using Xunit;

namespace Parley.UnitTests.Campaigns.Holdout;

/// <summary>
/// The holdout assignment is a fixed function of seed and contact. The vectors are the shared ones of the domain model
/// (08 section 7.1) and of the Python reference; they must not change to make a test pass.
/// </summary>
public sealed class HoldoutAssignerTests
{
    private const string Seed = "0192f5a0-7b1c-7c3e-8a10-3f6d2b9e4c11";

    private const int DefaultShareBps = 1000;

    private const int StatisticalSampleSize = 100_000;

    private const int StatisticalSampleSeed = 20261107;

    private const int GuidByteLength = 16;

    /// <summary>Half-width of the accepted holdout share, in basis points: about four standard errors at 100,000 ids.</summary>
    private const int ShareToleranceBps = 40;

    public static TheoryData<Guid, int, string> SharedVectors => new()
    {
        { Guid.Parse("0192f5a1-0000-7000-8000-000000000001"), 3443, HoldoutArms.Treatment },
        { Guid.Parse("0192f5a1-0000-7000-8000-000000000002"), 8750, HoldoutArms.Treatment },
        { Guid.Parse("0192f5a1-0000-7000-8000-000000000003"), 8290, HoldoutArms.Treatment },
        { Guid.Parse("0192f5a1-0000-7000-8000-00000000000a"), 6242, HoldoutArms.Treatment },
        { Guid.Parse("0192f5a1-0000-7000-8000-0000000000ff"), 2771, HoldoutArms.Treatment },
        { Guid.Parse("0192f5a1-0000-7000-8000-000000000006"), 883, HoldoutArms.Holdout },
        { Guid.Parse("0192f5a1-0000-7000-8000-00000000001b"), 134, HoldoutArms.Holdout },
    };

    [Theory]
    [MemberData(nameof(SharedVectors))]
    public void SharedVectors_MatchTheReferenceBucketsAndArms(Guid contactId, int expectedBucket, string expectedArm)
    {
        HoldoutAssigner.Bucket(Seed, contactId).Should().Be(expectedBucket);
        HoldoutAssigner.Assign(Seed, contactId, DefaultShareBps).Should().Be(expectedArm);
    }

    [Fact]
    public void Assignment_IsIndependentOfProcessingOrder_AndSharesMatchTheConfiguredShare()
    {
        var random = new Random(StatisticalSampleSeed);
        var contactIds = Enumerable.Range(0, StatisticalSampleSize)
            .Select(_ => NextGuid(random))
            .ToArray();

        var forward = contactIds.Select(id => HoldoutAssigner.Assign(Seed, id, DefaultShareBps)).ToArray();
        var backward = contactIds.Reverse().Select(id => HoldoutAssigner.Assign(Seed, id, DefaultShareBps)).Reverse().ToArray();

        forward.Should().Equal(backward, "the arm depends on seed and contact only");

        var holdoutCount = forward.Count(arm => arm == HoldoutArms.Holdout);
        var observedBps = holdoutCount * (double)BasisPoints.Scale / StatisticalSampleSize;
        observedBps.Should().BeApproximately(DefaultShareBps, ShareToleranceBps);
    }

    [Fact]
    public void Assignment_WithAnotherSeed_IsIndependent()
    {
        var contactId = Guid.Parse("0192f5a1-0000-7000-8000-000000000001");

        HoldoutAssigner.Bucket(Seed, contactId).Should().NotBe(HoldoutAssigner.Bucket(Seed + "-other", contactId));
    }

    private static Guid NextGuid(Random random)
    {
        var bytes = new byte[GuidByteLength];
        random.NextBytes(bytes);
        return new Guid(bytes);
    }
}
