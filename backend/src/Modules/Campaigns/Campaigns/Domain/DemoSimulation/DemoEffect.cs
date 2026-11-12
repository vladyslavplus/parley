using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Parley.Campaigns.Domain.DemoSimulation;

/// <summary>
/// The documented effect function of the demo campaigns: tau(x) from the segments of <c>ml/configs/crm_uplift_demo.yaml</c>,
/// the first matching segment wins, and the outcome rule of the reference module (<c>demo_effect.treated_outcome</c>).
/// The rules are written out here, in the same order and with the same thresholds; the unit tests pin the values that
/// <c>ml/tests/test_demo_effect.py</c> computes for the same inputs.
/// </summary>
public static class DemoEffect
{
    /// <summary>Seed of the demo, the same value as the configuration file. Re-seeding with it gives identical demo rows.</summary>
    public const long Seed = 20261115;

    public const string DoNotDisturb = "do_not_disturb";

    public const string LostCause = "lost_cause";

    public const string SureThing = "sure_thing";

    public const string Persuadable = "persuadable";

    public const string Other = "other";

    private const int DrawBytes = 8;

    private const int MantissaDroppedBits = 11;

    private const double MantissaScale = 1d / (1L << 53);

    private static readonly DemoSegment[] Segments =
    [
        new(DoNotDisturb, -0.30, features => features.OrderCount == 1 && features.RecencyDays <= 90),
        new(LostCause, 0.01, features => features.RecencyDays > 365),
        new(SureThing, 0d, features => features.RecencyDays <= 30 && features.Orders365Days >= 6 && features.Value365Days >= 1000),
        new(Persuadable, 0.30, features => features.RecencyDays > 30 && features.RecencyDays <= 180
            && features.Orders365Days >= 2 && features.Orders365Days <= 3),
    ];

    private const double DefaultTau = 0d;

    /// <summary>Name of the first matching segment, or <see cref="Other"/> when none matches.</summary>
    public static string SegmentOf(DemoContactFeatures features) =>
        Segments.FirstOrDefault(segment => segment.Matches(features))?.Name ?? Other;

    /// <summary>The effect on the conversion probability of one contact.</summary>
    public static double Tau(DemoContactFeatures features) =>
        Segments.FirstOrDefault(segment => segment.Matches(features))?.Tau ?? DefaultTau;

    /// <summary>
    /// Outcome under treatment from the baseline outcome, the effect and a uniform draw in [0, 1). A positive effect turns
    /// a non-buyer into a buyer when the draw is below it. A negative effect would remove a buyer when the draw is below its
    /// size; the demo cannot remove a real order, so the seed never writes that case (see the demo seeding notes).
    /// </summary>
    public static int TreatedOutcome(int baseline, double effect, double draw)
    {
        if (effect > 0d && baseline == 0 && draw < effect)
        {
            return 1;
        }

        if (effect < 0d && baseline == 1 && draw < -effect)
        {
            return 0;
        }

        return baseline;
    }

    /// <summary>
    /// A deterministic uniform value in [0, 1) for one purpose of one contact in one campaign. It is the first eight bytes
    /// of SHA-256 over the seed, the campaign, the contact and the purpose, so a re-seed gives the same draws.
    /// </summary>
    public static double Draw(Guid campaignId, Guid contactId, string purpose)
    {
        var input = Encoding.UTF8.GetBytes($"{Seed}:{campaignId:N}:{contactId:N}:{purpose}");
        var hash = SHA256.HashData(input);
        var bits = BinaryPrimitives.ReadUInt64BigEndian(hash.AsSpan(0, DrawBytes));

        // The top 53 bits form a double mantissa exactly, so the result is below 1 and never rounds up to it.
        return (bits >> MantissaDroppedBits) * MantissaScale;
    }

    private sealed record DemoSegment(string Name, double Tau, Func<DemoContactFeatures, bool> Matches);
}
