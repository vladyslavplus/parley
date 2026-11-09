using System.Text.RegularExpressions;
using Npgsql;

namespace Parley.Api.Cli.SeedDemo;

/// <summary>
/// Decides whether <c>seed-demo</c> may write to the database in the connection string. Demo runs against any database
/// the Demo environment points to. Development needs three things at once: the confirmation switch, a local host and a
/// demo database name. The name check is the real protection: a development configuration that points at a personal
/// database is refused even when the other two are given.
/// </summary>
public static class SeedDemoTargetGuard
{
    private static readonly string[] LoopbackHosts = ["localhost", "127.0.0.1", "::1"];

    private static readonly Regex DemoDatabaseName = new(
        SeedDemoCommandConstants.DemoDatabaseNamePattern,
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static SeedDemoTargetDecision Evaluate(IHostEnvironment environment, string connectionString, bool confirmed)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (environment.IsEnvironment(SeedDemoCommandConstants.DemoEnvironmentName))
        {
            return SeedDemoTargetDecision.Allow;
        }

        if (!environment.IsDevelopment())
        {
            return SeedDemoTargetDecision.Refuse(
                $"{SeedDemoCommandConstants.Verb} runs only in the Development or {SeedDemoCommandConstants.DemoEnvironmentName} environment.");
        }

        if (!confirmed)
        {
            return SeedDemoTargetDecision.Refuse(
                $"{SeedDemoCommandConstants.Verb} in Development needs {SeedDemoCommandConstants.ConfirmTargetOption}, "
                + "because Development configuration can point at a personal database.");
        }

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        if (!IsLocalHost(builder.Host))
        {
            return SeedDemoTargetDecision.Refuse(
                $"{SeedDemoCommandConstants.Verb} in Development writes only to a loopback host (localhost, 127.0.0.1 or ::1).");
        }

        if (!IsDemoDatabaseName(builder.Database))
        {
            return SeedDemoTargetDecision.Refuse(
                $"{SeedDemoCommandConstants.Verb} in Development writes only to a database named "
                + $"{SeedDemoCommandConstants.DemoDatabaseNamePrefix} or {SeedDemoCommandConstants.DemoDatabaseNamePrefix}_<suffix>.");
        }

        return SeedDemoTargetDecision.Allow;
    }

    /// <summary>True for <c>parley_demo</c> and <c>parley_demo_&lt;suffix&gt;</c>; any other name, including the development database, is not a demo database.</summary>
    public static bool IsDemoDatabaseName(string? database) =>
        !string.IsNullOrEmpty(database) && DemoDatabaseName.IsMatch(database);

    /// <summary>
    /// Local means the loopback names only: <c>localhost</c>, <c>127.0.0.1</c> and <c>::1</c>, optionally in brackets.
    /// A single-label name (which can resolve to another machine), a list, a socket path or any other address is not local.
    /// </summary>
    public static bool IsLocalHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        var name = host.Trim();
        if (name.Length > 2 && name[0] == '[' && name[^1] == ']')
        {
            name = name[1..^1];
        }

        return LoopbackHosts.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Host, port and database of the target. The password is never part of the text.</summary>
    public static string Describe(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        return $"host {builder.Host}, port {builder.Port}, database {builder.Database}";
    }
}
