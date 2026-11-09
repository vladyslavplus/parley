namespace Parley.Api.Cli.SeedDemo;

/// <summary>
/// Resolves the paths of <c>seed-demo</c> against the repository root. <c>dotnet run --project</c> starts the host in the
/// project folder, so a relative path taken from the working directory would land in the wrong place.
/// </summary>
public static class SeedDemoPaths
{
    /// <summary>Marker that identifies the repository root: the backend solution sits in its <c>backend</c> folder.</summary>
    private const string SolutionPath = "backend/Parley.slnx";

    /// <summary>
    /// Finds the repository root by walking up from <paramref name="contentRootPath"/>. Falls back to the working directory
    /// when no ancestor holds the solution.
    /// </summary>
    public static string RepositoryRoot(string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);
        for (var directory = new DirectoryInfo(Path.GetFullPath(contentRootPath)); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionPath)))
            {
                return directory.FullName;
            }
        }

        return Directory.GetCurrentDirectory();
    }

    /// <summary>Absolute paths stay as they are; relative paths resolve against the repository root.</summary>
    public static string Resolve(string path, string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(repositoryRoot, path));
    }
}
