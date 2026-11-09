using AwesomeAssertions;
using Parley.Api.Cli.SeedDemo;
using Xunit;

namespace Parley.UnitTests.Api.Cli;

/// <summary>Relative paths of <c>seed-demo</c> resolve against the repository root, whatever the working directory is.</summary>
public sealed class SeedDemoPathsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "parley-paths-" + Guid.CreateVersion7().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public void Finds_the_repository_root_from_a_project_folder_below_it()
    {
        var backend = Path.Combine(_root, "backend");
        var project = Path.Combine(backend, "src", "Parley.Api");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(backend, "Parley.slnx"), string.Empty);

        SeedDemoPaths.RepositoryRoot(project).Should().Be(Path.GetFullPath(_root));
    }

    [Fact]
    public void Resolves_a_relative_path_against_the_root_and_keeps_an_absolute_one()
    {
        var absolute = Path.Combine(_root, "elsewhere", "accounts.txt");

        SeedDemoPaths.Resolve("artifacts/demo-accounts.txt", _root)
            .Should().Be(Path.GetFullPath(Path.Combine(_root, "artifacts", "demo-accounts.txt")));
        SeedDemoPaths.Resolve(absolute, _root).Should().Be(Path.GetFullPath(absolute));
    }
}
