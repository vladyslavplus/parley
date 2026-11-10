using System.Data.Common;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Parley.Api.Hangfire;
using Parley.Api.Options;
using Parley.Campaigns.Contracts.DemoSeed;
using Parley.Crm.Contracts.DemoSeed;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.DemoSeed;

namespace Parley.Api.Cli.SeedDemo;

/// <summary>
/// Runs <c>seed-demo</c> on the host: prints the target, checks it, proves the accounts file is writable, starts the host
/// (which applies the migrations in Development), creates the demo accounts and writes their new passwords to the private
/// file before anything else is written, then loads the export and prints the counts as JSON. Passwords never reach the output.
/// Relative paths resolve against the repository root.
/// </summary>
public static class SeedDemoCommand
{
    private const string PasswordRecoveryHint =
        "The accounts were created but their passwords could not be saved. Re-run with "
        + SeedDemoCommandConstants.ResetPasswordsOption + " to issue new passwords.";

    private static readonly JsonSerializerOptions OutputOptions = new() { WriteIndented = true };

    public static async Task<int> RunAsync(WebApplication app, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!SeedDemoArguments.TryParse(args, out var arguments, out var usageError) || arguments is null)
        {
            await Console.Error.WriteLineAsync(usageError);
            return SeedDemoCommandConstants.ExitUsage;
        }

        var connectionString = app.Services.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString;
        await Console.Out.WriteLineAsync($"Target: {SeedDemoTargetGuard.Describe(connectionString)}.");

        var decision = SeedDemoTargetGuard.Evaluate(app.Environment, connectionString, arguments.ConfirmTargetDatabase);
        if (!decision.Allowed)
        {
            await Console.Error.WriteLineAsync(decision.Reason);
            return SeedDemoCommandConstants.ExitRefused;
        }

        var root = SeedDemoPaths.RepositoryRoot(app.Environment.ContentRootPath);
        var inputDirectory = SeedDemoPaths.Resolve(arguments.InputDirectory, root);
        var accountsFile = SeedDemoPaths.Resolve(arguments.AccountsFile, root);

        if (!Directory.Exists(inputDirectory))
        {
            await Console.Error.WriteLineAsync("The input directory does not exist.");
            return SeedDemoCommandConstants.ExitUsage;
        }

        // Checked before any row is written: a password must never be created without a file that can hold it.
        try
        {
            SeedDemoAccountsFile.VerifyWritable(accountsFile);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await Console.Error.WriteLineAsync($"The accounts file is not writable: {exception.Message}");
            return SeedDemoCommandConstants.ExitFailed;
        }

        try
        {
            // Starting the host applies the migrations in Development, so a database failure surfaces here as well.
            await app.StartAsync(cancellationToken);

            // The seed runs no hosted services, so the Hangfire tables the campaign seed enqueues into are installed here.
            await HangfireSchemaInstaller.InstallAsync(connectionString, cancellationToken);
            await using var scope = app.Services.CreateAsyncScope();
            var identity = await scope.ServiceProvider.GetRequiredService<IIdentityDemoDataImporter>()
                .ImportAsync(SeedDemoOrganization.Profile, arguments.ResetPasswords, cancellationToken);

            var accountsWritten = WriteAccountsFile(accountsFile, identity);

            var owners = identity.Accounts
                .Where(account => account.RoleCode == SystemRoles.SalesRep)
                .Select(account => account.UserId)
                .ToArray();
            var crm = await scope.ServiceProvider.GetRequiredService<ICrmDemoDataImporter>().ImportAsync(
                new CrmDemoImportRequest(
                    SeedDemoOrganization.Id,
                    inputDirectory,
                    SeedDemoOrganization.Currency,
                    SeedDemoOrganization.Locale,
                    owners),
                cancellationToken);

            // The historic campaigns are dated back from the last order of the import, the end of the dataset timeline.
            var datasetEnd = crm.LatestOrderAt
                ?? throw new InvalidOperationException("The CRM import has no orders, so the historic demo campaigns have no dataset end.");
            var campaigns = await scope.ServiceProvider.GetRequiredService<ICampaignDemoSeeder>()
                .SeedAsync(SeedDemoOrganization.Id, datasetEnd, cancellationToken);

            var summary = new SeedDemoSummary(
                identity.OrganizationCreated,
                identity.TeamsCreated,
                identity.UsersCreated,
                identity.MembershipsCreated,
                accountsWritten ? accountsFile : null,
                crm,
                campaigns);
            await Console.Out.WriteLineAsync(JsonSerializer.Serialize(summary, OutputOptions));
            return SeedDemoCommandConstants.ExitSucceeded;
        }
        catch (Exception exception) when (exception is DbException or DbUpdateException)
        {
            // Only the code and the database's own message: no stack trace, no connection string.
            await Console.Error.WriteLineAsync(
                $"{SeedDemoCommandConstants.DatabaseErrorCode}: the database rejected the seed. Check that it is reachable and that "
                + $"its migrations can run. Detail: {exception.Message}");
            return SeedDemoCommandConstants.ExitFailed;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or FileNotFoundException or InvalidOperationException)
        {
            await Console.Error.WriteLineAsync($"The demo data could not be seeded: {exception.Message}");
            return SeedDemoCommandConstants.ExitFailed;
        }
        finally
        {
            // The shutdown runs after the run was cancelled or failed, so it must not reuse that token.
            await app.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// Writes the new passwords before the CRM import. When the write fails, the accounts exist without a saved password,
    /// so the run stops and tells the operator how to recover. Warns when existing accounts have no password file.
    /// </summary>
    private static bool WriteAccountsFile(string accountsFile, IdentityDemoSeedResult identity)
    {
        bool written;
        try
        {
            written = SeedDemoAccountsFile.WriteIfCreated(accountsFile, identity.Accounts);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new InvalidOperationException($"{PasswordRecoveryHint} Cause: {exception.Message}", exception);
        }

        if (!written && !File.Exists(accountsFile))
        {
            Console.Error.WriteLine(
                "Warning: the demo accounts already exist, but no password file was found. "
                + $"Re-run with {SeedDemoCommandConstants.ResetPasswordsOption} to issue new passwords.");
        }

        return written;
    }
}
