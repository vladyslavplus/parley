using System.Globalization;

namespace Parley.Api.Cli.SeedDemo;

/// <summary>
/// Parsed arguments of <c>seed-demo --input &lt;dir&gt; [--accounts-file &lt;path&gt;] [--confirm-target-database] [--reset-passwords]</c>.
/// </summary>
/// <param name="InputDirectory">Directory with the demo export files.</param>
/// <param name="AccountsFile">File that receives the generated demo passwords.</param>
/// <param name="ConfirmTargetDatabase">True when the operator confirmed the target database (needed in Development).</param>
/// <param name="ResetPasswords">True when existing demo accounts get new passwords, which are then written to the accounts file.</param>
public sealed record SeedDemoArguments(string InputDirectory, string AccountsFile, bool ConfirmTargetDatabase, bool ResetPasswords)
{
    /// <summary>Reads the options that follow the verb. Every option takes one value except the switches.</summary>
    public static bool TryParse(IReadOnlyList<string> args, out SeedDemoArguments? parsed, out string? error)
    {
        string? input = null;
        string? accountsFile = null;
        var confirmed = false;
        var resetPasswords = false;
        for (var index = 0; index < args.Count; index++)
        {
            var name = args[index];
            if (name == SeedDemoCommandConstants.ConfirmTargetOption)
            {
                confirmed = true;
                continue;
            }

            if (name == SeedDemoCommandConstants.ResetPasswordsOption)
            {
                resetPasswords = true;
                continue;
            }

            if (index + 1 >= args.Count)
            {
                parsed = null;
                error = string.Format(CultureInfo.InvariantCulture, "The option {0} needs a value.", name);
                return false;
            }

            var value = args[++index];
            if (name == SeedDemoCommandConstants.InputOption)
            {
                input = value;
            }
            else if (name == SeedDemoCommandConstants.AccountsFileOption)
            {
                accountsFile = value;
            }
            else
            {
                parsed = null;
                error = string.Format(CultureInfo.InvariantCulture, "Unknown option {0}.", name);
                return false;
            }
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            parsed = null;
            error = string.Format(
                CultureInfo.InvariantCulture,
                "Usage: {0} {1} <directory> [{2} <file>] [{3}] [{4}]",
                SeedDemoCommandConstants.Verb,
                SeedDemoCommandConstants.InputOption,
                SeedDemoCommandConstants.AccountsFileOption,
                SeedDemoCommandConstants.ConfirmTargetOption,
                SeedDemoCommandConstants.ResetPasswordsOption);
            return false;
        }

        parsed = new SeedDemoArguments(input, accountsFile ?? SeedDemoCommandConstants.DefaultAccountsFile, confirmed, resetPasswords);
        error = null;
        return true;
    }
}
