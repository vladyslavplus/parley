using AwesomeAssertions;
using Parley.Crm.Domain.Pipelines;
using Parley.Crm.Shared;
using Parley.Crm.Shared.Pipelines;
using Parley.SharedKernel.Localization;
using Xunit;

namespace Parley.UnitTests.Crm.Pipelines;

/// <summary>The default pipeline every organization starts with keeps the stage invariants and has names in every locale.</summary>
public sealed class DefaultPipelineStagesTests
{
    [Fact]
    public void DefaultPipeline_HasOneWonAndOneLost_ClosingTheOpenStages()
    {
        var kinds = DefaultPipelineStages.All.Select(stage => stage.Kind).ToArray();

        kinds.Count(kind => kind == PipelineStageKinds.Open).Should().BeGreaterThanOrEqualTo(1);
        kinds.Count(kind => kind == PipelineStageKinds.Won).Should().Be(1);
        kinds.Count(kind => kind == PipelineStageKinds.Lost).Should().Be(1);
        kinds.TakeLast(2).Should().Equal(PipelineStageKinds.Won, PipelineStageKinds.Lost);
        kinds.Length.Should().BeLessThanOrEqualTo(CrmLimits.PipelineStageMaximumCount);
    }

    [Theory]
    [InlineData(CultureCodes.Ukrainian)]
    [InlineData(CultureCodes.English)]
    public void DefaultStageNames_ExistInEveryLocale(string culture)
    {
        foreach (var stage in DefaultPipelineStages.All)
        {
            SharedTexts.Get(stage.NameKey, culture).Should().NotBe(stage.NameKey);
        }

        SharedTexts.Get(DefaultPipelineStages.PipelineNameKey, culture).Should().NotBe(DefaultPipelineStages.PipelineNameKey);
    }
}
