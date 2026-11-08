using Parley.Identity.Contracts.Authorization;

namespace Parley.IntegrationTests.Access.Support;

/// <summary>
/// Test-only request guarded by <see cref="Permissions.ContactsWrite"/>. Registered as an
/// application part by <see cref="Parley.IntegrationTests.Infrastructure.Hosting.IdentityApiFactory"/>; it never ships in the API host.
/// </summary>
public static class PermissionProbeRoutes
{
    public const string Probe = "test-only/permission-probe";
}
