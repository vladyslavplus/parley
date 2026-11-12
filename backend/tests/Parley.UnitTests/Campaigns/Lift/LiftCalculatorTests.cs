using System.Text.Json;
using AwesomeAssertions;
using Parley.Campaigns.Domain.Holdout;
using Parley.Campaigns.Domain.Lift;
using Parley.Campaigns.Options;
using Xunit;

namespace Parley.UnitTests.Campaigns.Lift;

/// <summary>
/// The lift statistics against every committed vector of the reference module (ml/tests/fixtures/lift_vectors.json). The
/// fixture is read from its one location in the repository, so the reference and the calculator cannot drift apart.
/// </summary>
public sealed class LiftCalculatorTests
{
    private const string FixtureRelativePath = "ml/tests/fixtures/lift_vectors.json";

    private const double RelativeTolerance = 1e-9;

    private const double AbsoluteTolerance = 1e-12;

    private static readonly CampaignsAnalysisOptions Analysis = new();

    public static TheoryData<string> VectorIds()
    {
        var data = new TheoryData<string>();
        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath()));
        foreach (var testCase in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            data.Add(testCase.GetProperty("id").GetString()!);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(VectorIds))]
    public void Statistics_MatchTheCommittedVector(string caseId)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FixturePath()));
        var testCase = document.RootElement.GetProperty("cases").EnumerateArray()
            .Single(item => item.GetProperty("id").GetString() == caseId);

        var result = Analyze(testCase);
        var expected = testCase.GetProperty("expected");
        var values = new Dictionary<string, double?>
        {
            ["rate_treatment"] = result.RateTreatment,
            ["rate_holdout"] = result.RateHoldout,
            ["lift"] = result.Lift,
            ["ci_low"] = result.CiLow,
            ["ci_high"] = result.CiHigh,
            ["z_test_p_value"] = result.ZTestPValue,
            ["srm_p_value"] = result.SrmPValue,
            ["mde_absolute"] = result.MinimumDetectableEffect,
            ["incremental_revenue"] = result.IncrementalRevenue,
        };

        foreach (var (field, computed) in values)
        {
            var reference = expected.GetProperty(field);
            if (reference.ValueKind == JsonValueKind.Null)
            {
                computed.Should().BeNull($"{caseId}.{field} is undefined in the reference");
                continue;
            }

            computed.Should().NotBeNull($"{caseId}.{field} is defined in the reference");
            var value = reference.GetDouble();
            computed!.Value.Should().BeApproximately(
                value,
                Math.Max(AbsoluteTolerance, RelativeTolerance * Math.Abs(value)),
                $"{caseId}.{field}");
        }
    }

    [Theory]
    [InlineData(0d, 1d)]
    [InlineData(0.5d, 0.4795001221869534d)]
    [InlineData(1d, 0.15729920705028513d)]
    [InlineData(2d, 0.004677734981047266d)]
    [InlineData(3d, 2.209049699858544e-05d)]
    public void Erfc_MatchesKnownValues(double x, double expected)
    {
        ErrorFunction.Erfc(x).Should().BeApproximately(expected, RelativeTolerance * expected);
    }

    [Fact]
    public void Analyze_IsUndefinedEverywhere_WhenBothArmsAreEmpty()
    {
        var result = LiftCalculator.Analyze(new LiftArm(0, 0, 0), new LiftArm(0, 0, 0), 5000, Analysis.ZAlpha, Analysis.ZBeta);

        result.Lift.Should().BeNull();
        result.SrmPValue.Should().BeNull("an empty experiment has no ratio to test");
    }

    private static LiftResult Analyze(JsonElement testCase)
    {
        var treatment = ReadArm(testCase.GetProperty("treatment"));
        var holdout = ReadArm(testCase.GetProperty("holdout"));
        var share = testCase.GetProperty("share_treatment_bps").GetInt32();
        return LiftCalculator.Analyze(treatment, holdout, share, Analysis.ZAlpha, Analysis.ZBeta);
    }

    private static LiftArm ReadArm(JsonElement arm)
    {
        var count = arm.GetProperty("n").GetInt64();
        var conversions = arm.GetProperty("conversions").GetInt64();
        var mean = arm.GetProperty("mean_revenue");

        // A mean is null only for an empty arm; the revenue sum is the mean times the count.
        var revenueSum = mean.ValueKind == JsonValueKind.Null ? 0d : mean.GetDouble() * count;
        return new LiftArm(count, conversions, revenueSum);
    }

    /// <summary>Finds the fixture above the test output, so the test runs from the build folder and from the repository alike.</summary>
    private static string FixturePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, FixtureRelativePath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"The statistics fixture ({FixtureRelativePath}) was not found above the test output.");
    }
}
