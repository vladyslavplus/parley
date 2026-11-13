using AwesomeAssertions;
using Parley.Insights.Domain.Ml;
using Parley.Insights.Features.Scoring.Shared;
using Xunit;

namespace Parley.UnitTests.Insights.Scoring;

/// <summary>The automatic model choice: no stub unless an administrator named it, and a stable order for equal dates.</summary>
public sealed class ActiveModelResolverTests
{
    private static readonly DateTime TrainedAt = new(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Select_AutoChoice_SkipsAStub()
    {
        var stub = Model("crm_uplift-stub", ScoringProvenances.Stub);
        var trained = Model("crm_uplift-trained", ScoringProvenances.SemiSynthetic);

        ActiveModelResolver.Select([stub, trained], configuredVersion: null)!.Version.Should().Be(trained.Version);
    }

    [Fact]
    public void Select_AnExplicitChoice_MayBeAStub()
    {
        var stub = Model("crm_uplift-stub", ScoringProvenances.Stub);
        var trained = Model("crm_uplift-trained", ScoringProvenances.SemiSynthetic);

        ActiveModelResolver.Select([stub, trained], stub.Version)!.Version.Should().Be(stub.Version);
    }

    [Fact]
    public void Select_EqualDates_OrderByTheVersion()
    {
        var first = Model("crm_uplift-a", ScoringProvenances.SemiSynthetic);
        var second = Model("crm_uplift-b", ScoringProvenances.SemiSynthetic);

        ActiveModelResolver.Select([first, second], configuredVersion: null)!.Version.Should().Be(second.Version);
    }

    [Fact]
    public void Select_NoAvailableModel_ReturnsNull()
    {
        ActiveModelResolver.Select([], configuredVersion: null).Should().BeNull();
    }

    private static ModelVersion Model(string version, string provenance) => new()
    {
        ModelName = ScoringModelNames.Uplift,
        Version = version,
        Kind = "uplift",
        Provenance = provenance,
        TrainedAt = TrainedAt,
    };
}
