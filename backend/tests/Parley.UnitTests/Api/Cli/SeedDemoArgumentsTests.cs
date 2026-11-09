using AwesomeAssertions;
using Parley.Api.Cli.SeedDemo;
using Xunit;

namespace Parley.UnitTests.Api.Cli;

/// <summary>Option parsing of <c>seed-demo</c>: values come in pairs, the switches stand alone.</summary>
public sealed class SeedDemoArgumentsTests
{
    [Fact]
    public void Parses_input_with_defaults_for_the_accounts_file_and_no_switches()
    {
        var parsed = SeedDemoArguments.TryParse(["--input", "export"], out var arguments, out var error);

        parsed.Should().BeTrue();
        error.Should().BeNull();
        arguments.Should().Be(new SeedDemoArguments(
            "export",
            SeedDemoCommandConstants.DefaultAccountsFile,
            ConfirmTargetDatabase: false,
            ResetPasswords: false));
    }

    [Fact]
    public void Parses_both_switches_and_an_accounts_file()
    {
        var parsed = SeedDemoArguments.TryParse(
            ["--confirm-target-database", "--input", "export", "--reset-passwords", "--accounts-file", "out/accounts.txt"],
            out var arguments,
            out _);

        parsed.Should().BeTrue();
        arguments.Should().Be(new SeedDemoArguments("export", "out/accounts.txt", ConfirmTargetDatabase: true, ResetPasswords: true));
    }

    [Fact]
    public void Refuses_a_missing_input_directory()
    {
        var parsed = SeedDemoArguments.TryParse(["--accounts-file", "out.txt"], out var arguments, out var error);

        parsed.Should().BeFalse();
        arguments.Should().BeNull();
        error.Should().Contain(SeedDemoCommandConstants.InputOption);
    }

    [Fact]
    public void Refuses_an_option_without_its_value()
    {
        var parsed = SeedDemoArguments.TryParse(["--input"], out _, out var error);

        parsed.Should().BeFalse();
        error.Should().Contain("needs a value");
    }

    [Fact]
    public void Refuses_an_unknown_option()
    {
        var parsed = SeedDemoArguments.TryParse(["--input", "export", "--organization-id", "x"], out _, out var error);

        parsed.Should().BeFalse();
        error.Should().Contain("Unknown option");
    }
}
