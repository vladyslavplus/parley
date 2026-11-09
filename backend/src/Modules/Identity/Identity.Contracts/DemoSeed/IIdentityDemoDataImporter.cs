namespace Parley.Identity.Contracts.DemoSeed;

/// <summary>
/// Creates the demo organization, its teams, its accounts and their memberships. Each step reuses rows that exist, so
/// a re-run creates nothing. Existing accounts keep their passwords unless the reset flag is set, in which case each
/// existing demo account gets a new generated password. The host checks the environment first.
/// </summary>
public interface IIdentityDemoDataImporter
{
    /// <summary>Runs the import; <paramref name="resetPasswords"/> issues new passwords for the accounts that already exist.</summary>
    Task<IdentityDemoSeedResult> ImportAsync(
        DemoOrganizationProfile organization,
        bool resetPasswords,
        CancellationToken cancellationToken);
}
