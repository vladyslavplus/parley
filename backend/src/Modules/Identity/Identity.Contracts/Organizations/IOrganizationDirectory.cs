namespace Parley.Identity.Contracts.Organizations;

/// <summary>
/// Platform-wide read of the organizations for scheduled jobs: the id and the time zone of each. It crosses tenants on
/// purpose, so only platform jobs may call it; user-facing endpoints never do.
/// </summary>
public interface IOrganizationDirectory
{
    /// <summary>Every organization, with the time zone the organization calendar uses.</summary>
    Task<IReadOnlyList<OrganizationTimeZone>> ListAsync(CancellationToken cancellationToken);
}
