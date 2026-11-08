using System.Reflection;
using System.Xml.Linq;
using AwesomeAssertions;
using Xunit;

namespace Parley.UnitTests.Architecture;

public sealed class ModuleBoundaryTests
{
    private static readonly string[] ModuleAssemblyNames =
    [
        "Parley.Identity",
        "Parley.Crm",
        "Parley.Campaigns",
        "Parley.Insights",
        "Parley.Notifications",
    ];

    [Fact]
    public void Modules_DoNotReferenceOtherModuleImplementations()
    {
        foreach (var moduleAssemblyName in ModuleAssemblyNames)
        {
            var assembly = Assembly.Load(moduleAssemblyName);
            var referenced = assembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);

            foreach (var other in ModuleAssemblyNames)
            {
                if (!string.Equals(other, moduleAssemblyName, StringComparison.Ordinal))
                {
                    referenced.Should().NotContain(other,
                        $"{moduleAssemblyName} must reach {other} through its .Contracts assembly only");
                }
            }

            var contractsAssembly = Assembly.Load($"{moduleAssemblyName}.Contracts");
            var contractsReferenced = contractsAssembly.GetReferencedAssemblies().Select(a => a.Name).ToHashSet(StringComparer.Ordinal);
            contractsReferenced.Should().NotIntersectWith(ModuleAssemblyNames,
                $"{moduleAssemblyName}.Contracts must stay independent of module implementations");
        }
    }

    [Fact]
    public void ModuleProjects_MayReferenceOnlyContractsAndSharedKernel()
    {
        var moduleProjects = FindModuleProjectFiles().Where(p => !p.EndsWith(".Contracts.csproj", StringComparison.OrdinalIgnoreCase)).ToArray();
        moduleProjects.Should().HaveCount(5, "there are five module implementation projects");

        foreach (var projectProjectPath in moduleProjects)
        {
            var sourceProject = Path.GetFileNameWithoutExtension(projectProjectPath);
            var references = ReadProjectReferences(projectProjectPath);

            foreach (var reference in references)
            {
                var allowed = string.Equals(reference, "Parley.SharedKernel", StringComparison.Ordinal)
                    || reference.EndsWith(".Contracts", StringComparison.Ordinal);
                allowed.Should().BeTrue(
                    $"{sourceProject} must not reference {reference}; cross-module access goes through .Contracts projects");
            }
        }
    }

    [Fact]
    public void ContractsProjects_MayReferenceOnlySharedKernelAndContracts()
    {
        var contractsProjects = FindModuleProjectFiles().Where(p => p.EndsWith(".Contracts.csproj", StringComparison.OrdinalIgnoreCase)).ToArray();
        contractsProjects.Should().HaveCount(5, "there are five module contract projects");

        foreach (var projectProjectPath in contractsProjects)
        {
            var sourceProject = Path.GetFileNameWithoutExtension(projectProjectPath);
            var references = ReadProjectReferences(projectProjectPath);

            foreach (var reference in references)
            {
                var allowed = string.Equals(reference, "Parley.SharedKernel", StringComparison.Ordinal)
                    || reference.EndsWith(".Contracts", StringComparison.Ordinal);
                allowed.Should().BeTrue(
                    $"{sourceProject} must not reference {reference}; contracts depend only on the shared kernel or other contracts");
            }
        }
    }

    private static string FindModulesDirectory()
    {
        var candidates = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory(),
        };

        foreach (var candidate in candidates)
        {
            var current = candidate;
            while (!string.IsNullOrEmpty(current))
            {
                var withBackend = Path.Combine(current, "backend", "src", "Modules");
                if (Directory.Exists(withBackend))
                {
                    return withBackend;
                }

                var direct = Path.Combine(current, "src", "Modules");
                if (Directory.Exists(direct))
                {
                    return direct;
                }

                current = Directory.GetParent(current)?.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate src/Modules from the test output directory.");
    }

    private static string[] FindModuleProjectFiles()
    {
        return Directory.GetFiles(FindModulesDirectory(), "*.csproj", SearchOption.AllDirectories);
    }

    private static string[] ReadProjectReferences(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants("ProjectReference")
            .Select(element => element.Attribute("Include")?.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => Path.GetFileNameWithoutExtension(value!.Replace('/', Path.DirectorySeparatorChar)))
            .ToArray();
    }
}
