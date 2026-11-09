using AwesomeAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Parley.Api.Cli.SeedDemo;
using Xunit;

namespace Parley.UnitTests.Api.Cli;

/// <summary>
/// The demo seed writes fake customers and accounts. Demo runs it freely. Development runs it only with the confirmation
/// switch, on a local host, into a database whose name is a demo name.
/// </summary>
public sealed class SeedDemoTargetGuardTests
{
    private const string LocalDemoConnection = "Host=127.0.0.1;Port=5432;Database=parley_demo;Username=u;Password=secret";

    [Theory]
    [InlineData(false, "localhost")]
    [InlineData(true, "db.example.com")]
    public void Demo_environment_allows_the_seed_for_any_host_and_database(bool confirmed, string host)
    {
        var decision = SeedDemoTargetGuard.Evaluate(
            new FakeHostEnvironment("Demo"),
            $"Host={host};Port=5432;Database=anything;Username=u;Password=p",
            confirmed);

        decision.Allowed.Should().BeTrue();
        decision.Reason.Should().BeNull();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Other_environments_refuse_the_seed_even_when_confirmed(string environment)
    {
        var decision = SeedDemoTargetGuard.Evaluate(new FakeHostEnvironment(environment), LocalDemoConnection, confirmed: true);

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain("Development or Demo");
    }

    [Fact]
    public void Development_refuses_the_seed_without_the_confirmation_switch()
    {
        var decision = SeedDemoTargetGuard.Evaluate(new FakeHostEnvironment(Environments.Development), LocalDemoConnection, confirmed: false);

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain(SeedDemoCommandConstants.ConfirmTargetOption);
    }

    [Theory]
    [InlineData("db.example.com")]
    [InlineData("10.0.0.5")]
    [InlineData("localhost,db.example.com")]
    public void Development_refuses_a_non_local_host_even_when_confirmed(string host)
    {
        var decision = SeedDemoTargetGuard.Evaluate(
            new FakeHostEnvironment(Environments.Development),
            $"Host={host};Port=5432;Database=parley_demo;Username=u;Password=p",
            confirmed: true);

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain("loopback host");
    }

    [Theory]
    [InlineData("parley")]
    [InlineData("parley_dev")]
    [InlineData("parley_demo_")]
    [InlineData("Parley_Demo")]
    [InlineData("demo")]
    public void Development_refuses_a_database_that_is_not_a_demo_database_even_when_confirmed_on_a_local_host(string database)
    {
        var decision = SeedDemoTargetGuard.Evaluate(
            new FakeHostEnvironment(Environments.Development),
            $"Host=127.0.0.1;Port=5432;Database={database};Username=u;Password=p",
            confirmed: true);

        decision.Allowed.Should().BeFalse();
        decision.Reason.Should().Contain("parley_demo");
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("localhost")]
    [InlineData("::1")]
    public void Development_allows_the_seed_on_a_local_host_into_a_demo_database_when_confirmed(string host)
    {
        var decision = SeedDemoTargetGuard.Evaluate(
            new FakeHostEnvironment(Environments.Development),
            $"Host={host};Port=5432;Database=parley_demo_verify;Username=u;Password=p",
            confirmed: true);

        decision.Allowed.Should().BeTrue();
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("/var/run/postgresql", false)]
    [InlineData("db.example.com", false)]
    [InlineData("parley-db-1", false)]
    [InlineData("10.0.0.5", false)]
    [InlineData("127.0.0.2", false)]
    [InlineData("localhost,db.example.com", false)]
    [InlineData("LOCALHOST", true)]
    [InlineData("[::1]", true)]
    public void Local_host_rule_accepts_loopback_names_only(string host, bool expected)
    {
        SeedDemoTargetGuard.IsLocalHost(host).Should().Be(expected);
    }

    [Theory]
    [InlineData("parley_demo", true)]
    [InlineData("parley_demo_verify", true)]
    [InlineData("parley_demo_2026_11", true)]
    [InlineData("parley", false)]
    [InlineData("parley_demo-x", false)]
    [InlineData("xparley_demo", false)]
    [InlineData("parley_demo\n", false)]
    [InlineData("Parley_Demo", false)]
    [InlineData(null, false)]
    public void Demo_database_rule_accepts_only_parley_demo_names(string? database, bool expected)
    {
        SeedDemoTargetGuard.IsDemoDatabaseName(database).Should().Be(expected);
    }

    [Fact]
    public void Describe_names_host_port_and_database_without_the_password()
    {
        var text = SeedDemoTargetGuard.Describe(LocalDemoConnection);

        text.Should().Contain("127.0.0.1").And.Contain("parley_demo");
        text.Should().NotContain("secret");
    }

    private sealed class FakeHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Parley.Api";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
