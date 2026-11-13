using System.Globalization;
using AwesomeAssertions;
using Parley.Campaigns.Domain.DemoSimulation;
using Xunit;

namespace Parley.UnitTests.Campaigns.Lift;

/// <summary>
/// The demo effect against the values ml/tests/test_demo_effect.py computes for the same inputs, and the draw rule that
/// keeps the demo deterministic.
/// </summary>
public sealed class DemoEffectTests
{
    private static readonly Guid Campaign = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000c1");

    private static readonly Guid Contact = Guid.Parse("0192f5a0-7b1c-7000-8000-0000000000c2");

    [Theory]
    [InlineData(10, 1, 1, 40, DemoEffect.DoNotDisturb, -0.30)]
    [InlineData(400, 0, 3, 0, DemoEffect.LostCause, 0.01)]
    [InlineData(5, 8, 9, 2000, DemoEffect.SureThing, 0d)]
    [InlineData(90, 2, 2, 120, DemoEffect.Persuadable, 0.30)]
    [InlineData(90, 1, 2, 60, DemoEffect.Other, 0d)]
    [InlineData(200, 2, 2, 120, DemoEffect.Other, 0d)]
    public void EachSegment_HasItsDocumentedEffect(double recency, double orders365, double orderCount, double value365, string segment, double effect)
    {
        var features = Features(recency, orders365, orderCount, value365);

        DemoEffect.SegmentOf(features).Should().Be(segment);
        DemoEffect.Tau(features).Should().BeApproximately(effect, 1e-12);
    }

    [Fact]
    public void TheFirstMatchingSegment_Wins()
    {
        // Matches do_not_disturb (one order, recent) and sure_thing (six or more orders, recent, high value). The first wins.
        var features = Features(recency: 5, orders365: 6, orderCount: 1, value365: 2000);

        DemoEffect.SegmentOf(features).Should().Be(DemoEffect.DoNotDisturb);
        DemoEffect.Tau(features).Should().BeApproximately(-0.30, 1e-12);
    }

    [Fact]
    public void NoPurchases_MatchNoRecencySegment()
    {
        // NaN recency compares false with every bound, as in the reference module.
        var features = Features(recency: double.NaN, orders365: 0, orderCount: 0, value365: 0);

        DemoEffect.SegmentOf(features).Should().Be(DemoEffect.Other);
    }

    [Fact]
    public void PositiveEffect_AddsAConversionOnlyToNonBuyers()
    {
        DemoEffect.TreatedOutcome(0, 0.15, draw: 0.10).Should().Be(1);
        DemoEffect.TreatedOutcome(0, 0.15, draw: 0.50).Should().Be(0);
        DemoEffect.TreatedOutcome(1, 0.15, draw: 0.99).Should().Be(1);
    }

    [Fact]
    public void NegativeEffect_RemovesAConversionOnlyFromBuyers()
    {
        DemoEffect.TreatedOutcome(1, -0.03, draw: 0.01).Should().Be(0);
        DemoEffect.TreatedOutcome(1, -0.03, draw: 0.50).Should().Be(1);
        DemoEffect.TreatedOutcome(0, -0.03, draw: 0.01).Should().Be(0);
    }

    [Fact]
    public void ZeroEffect_LeavesTheOutcomeUnchanged()
    {
        DemoEffect.TreatedOutcome(1, 0d, draw: 0d).Should().Be(1);
        DemoEffect.TreatedOutcome(0, 0d, draw: 0d).Should().Be(0);
    }

    [Fact]
    public void Draw_IsDeterministic_AndBelowOne()
    {
        var first = DemoEffect.Draw(Campaign, Contact, "outcome");

        DemoEffect.Draw(Campaign, Contact, "outcome").Should().Be(first, "the same inputs give the same draw");
        DemoEffect.Draw(Campaign, Contact, "timing").Should().NotBe(first, "a different purpose gives a different draw");
        first.Should().BeGreaterThanOrEqualTo(0d).And.BeLessThan(1d);
    }

    /// <summary>
    /// The seed and the segment names and effects in ml/configs/crm_uplift_demo.yaml equal the code's. The file is read line by
    /// line (no YAML parser): the seed line, then each <c>- name:</c> entry with its <c>tau:</c> line in the effect block. Each
    /// segment is checked on one representative contact, which the segment's own rule admits.
    /// </summary>
    [Fact]
    public void ConfigFile_MatchesTheSeedAndTheSegmentsOfTheCode()
    {
        var lines = File.ReadAllLines(ConfigFile());

        ReadSeed(lines).Should().Be(DemoEffect.Seed);
        var segments = ReadSegments(lines);
        segments.Select(segment => segment.Name).Should().Equal(
            DemoEffect.DoNotDisturb,
            DemoEffect.LostCause,
            DemoEffect.SureThing,
            DemoEffect.Persuadable);

        var representatives = new Dictionary<string, DemoContactFeatures>
        {
            [DemoEffect.DoNotDisturb] = Features(10, 1, 1, 40),
            [DemoEffect.LostCause] = Features(400, 0, 3, 0),
            [DemoEffect.SureThing] = Features(5, 8, 9, 2000),
            [DemoEffect.Persuadable] = Features(90, 2, 2, 120),
        };
        foreach (var (name, tau) in segments)
        {
            var features = representatives[name];
            DemoEffect.SegmentOf(features).Should().Be(name);
            DemoEffect.Tau(features).Should().BeApproximately(tau, 1e-12, $"the config gives {name} an effect of {tau}");
        }
    }

    private static long ReadSeed(IReadOnlyList<string> lines)
    {
        var line = lines.First(text => text.StartsWith("seed:", StringComparison.Ordinal));
        return long.Parse(line["seed:".Length..].Trim(), CultureInfo.InvariantCulture);
    }

    /// <summary>Each <c>- name:</c> with the <c>tau:</c> that follows it, in file order.</summary>
    private static List<(string Name, double Tau)> ReadSegments(IReadOnlyList<string> lines)
    {
        var segments = new List<(string Name, double Tau)>();
        string? name = null;
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("- name:", StringComparison.Ordinal))
            {
                name = line["- name:".Length..].Trim();
            }
            else if (name is not null && line.StartsWith("tau:", StringComparison.Ordinal))
            {
                var value = line["tau:".Length..].Split('#')[0].Trim();
                segments.Add((name, double.Parse(value, CultureInfo.InvariantCulture)));
                name = null;
            }
        }

        return segments;
    }

    /// <summary>The config file, found by walking up from the test binary to the repository root.</summary>
    private static string ConfigFile()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "ml", "configs", "crm_uplift_demo.yaml");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("ml/configs/crm_uplift_demo.yaml was not found above the test binary.");
    }

    private static DemoContactFeatures Features(double recency, double orders365, double orderCount, double value365) =>
        new(
            RecencyDays: recency,
            TenureDays: 400d,
            Orders90Days: 0d,
            Orders365Days: orders365,
            OrderCount: orderCount,
            Value365Days: value365,
            AvgOrderValue: orders365 > 0 ? value365 / orders365 : 0d,
            HasOpenDeal: 0d);
}
