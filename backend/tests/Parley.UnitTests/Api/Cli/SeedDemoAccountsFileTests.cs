using AwesomeAssertions;
using Parley.Api.Cli.SeedDemo;
using Parley.Identity.Contracts.DemoSeed;
using Xunit;

namespace Parley.UnitTests.Api.Cli;

/// <summary>The password file: written only when passwords exist, written in one step, and checked before accounts are created.</summary>
public sealed class SeedDemoAccountsFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "parley-accounts-" + Guid.CreateVersion7().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void Writes_only_the_accounts_that_hold_a_password_and_leaves_no_temporary_file()
    {
        var path = Path.Combine(_directory, "accounts.txt");
        var accounts = new[]
        {
            new DemoAccount(Guid.CreateVersion7(), "admin@demo.parley.test", "Admin", "admin", null, "secret-one"),
            new DemoAccount(Guid.CreateVersion7(), "rep.north.1@demo.parley.test", "Rep", "sales_rep", "Team", null),
        };

        var written = SeedDemoAccountsFile.WriteIfCreated(path, accounts);

        written.Should().BeTrue();
        var text = File.ReadAllText(path);
        text.Should().Contain("admin@demo.parley.test\tadmin\t-\tsecret-one");
        text.Should().NotContain("rep.north.1@demo.parley.test");
        Directory.GetFiles(_directory).Should().BeEquivalentTo([path]);
    }

    [Fact]
    public void Writes_nothing_when_no_account_has_a_new_password()
    {
        var path = Path.Combine(_directory, "accounts.txt");
        var accounts = new[] { new DemoAccount(Guid.CreateVersion7(), "admin@demo.parley.test", "Admin", "admin", null, null) };

        SeedDemoAccountsFile.WriteIfCreated(path, accounts).Should().BeFalse();

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public void Verify_writable_creates_the_directory_and_leaves_no_probe_behind()
    {
        var path = Path.Combine(_directory, "nested", "accounts.txt");

        SeedDemoAccountsFile.VerifyWritable(path);

        Directory.Exists(Path.Combine(_directory, "nested")).Should().BeTrue();
        Directory.GetFiles(Path.Combine(_directory, "nested")).Should().BeEmpty();
    }

    [Fact]
    public void Verify_writable_fails_before_any_account_exists_when_a_parent_is_a_file()
    {
        Directory.CreateDirectory(_directory);
        var blocker = Path.Combine(_directory, "blocker");
        File.WriteAllText(blocker, "not a directory");
        var path = Path.Combine(blocker, "accounts.txt");

        var verify = () => SeedDemoAccountsFile.VerifyWritable(path);

        verify.Should().Throw<IOException>();
    }
}
