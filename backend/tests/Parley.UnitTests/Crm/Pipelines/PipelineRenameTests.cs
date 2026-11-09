using AwesomeAssertions;
using Parley.Crm.Domain.Pipelines;
using Xunit;

namespace Parley.UnitTests.Crm.Pipelines;

/// <summary>A system name becomes a typed name only when its text changes; keeping the shown or the stored text keeps the key.</summary>
public sealed class PipelineRenameTests
{
    private const string SystemKey = "crm.pipeline.stage.new";

    private const string StoredName = "Нові";

    private const string DisplayName = "New";

    private const string TypedName = "Discovery";

    [Fact]
    public void Stage_RenameToTypedText_ClearsSystemKey()
    {
        var stage = new PipelineStage { Name = StoredName, SystemKey = SystemKey };

        stage.Rename(TypedName, DisplayName);

        stage.Name.Should().Be(TypedName);
        stage.SystemKey.Should().BeNull();
    }

    [Theory]
    [InlineData(StoredName)]
    [InlineData(DisplayName)]
    public void Stage_RenameToShownOrStoredText_KeepsSystemKey(string text)
    {
        var stage = new PipelineStage { Name = StoredName, SystemKey = SystemKey };

        stage.Rename(text, DisplayName);

        stage.SystemKey.Should().Be(SystemKey);
    }

    [Fact]
    public void Pipeline_RenameToTypedText_ClearsSystemKey()
    {
        var pipeline = new Pipeline { Name = StoredName, SystemKey = SystemKey };

        pipeline.Rename(TypedName, DisplayName);

        pipeline.Name.Should().Be(TypedName);
        pipeline.SystemKey.Should().BeNull();
    }
}
