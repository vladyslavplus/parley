namespace Parley.Api.Cli.SeedDemo;

/// <summary>Verb, options, environment, database name rule and exit codes of the <c>seed-demo</c> command.</summary>
public static class SeedDemoCommandConstants
{
    public const string Verb = "seed-demo";

    public const string InputOption = "--input";

    public const string AccountsFileOption = "--accounts-file";

    /// <summary>Switch that acknowledges the database named in the connection string is meant to be seeded (Development only).</summary>
    public const string ConfirmTargetOption = "--confirm-target-database";

    /// <summary>Switch that issues new passwords for the demo accounts that already exist, for a lost password file.</summary>
    public const string ResetPasswordsOption = "--reset-passwords";

    /// <summary>Environment name that allows the demo seed without the confirmation switch.</summary>
    public const string DemoEnvironmentName = "Demo";

    /// <summary>Name of the demo database: <c>parley_demo</c>, or <c>parley_demo_&lt;suffix&gt;</c>. The reset script uses the same rule.</summary>
    public const string DemoDatabaseNamePrefix = "parley_demo";

    /// <summary>Full match, case-sensitive. <c>\z</c> rather than <c>$</c>, which also matches before a trailing newline.</summary>
    public const string DemoDatabaseNamePattern = "^parley_demo(_[a-z0-9_]+)?\\z";

    /// <summary>Where the generated demo passwords are written. The path is git-ignored.</summary>
    public const string DefaultAccountsFile = "artifacts/demo-accounts.txt";

    /// <summary>Loopback URL with port 0: the operating system picks a free port, so the seed never clashes with a running API.</summary>
    public const string LoopbackAnyPortUrl = "http://127.0.0.1:0";

    /// <summary>Stable code printed when the database refuses the seed (unreachable, migration or constraint failure).</summary>
    public const string DatabaseErrorCode = "seed_demo.database_error";

    public const int ExitSucceeded = 0;

    public const int ExitFailed = 1;

    public const int ExitUsage = 2;

    public const int ExitRefused = 3;
}
