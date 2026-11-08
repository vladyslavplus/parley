namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// Resolves the record visibility of a user for a permission from the membership
/// and the role grant. Usable from HTTP requests and background jobs alike, because
/// the user and the organization are passed explicitly.
/// </summary>
public interface IUserScopeReader
{
    /// <summary>
    /// Returns <see cref="DataScope.Nothing"/> when the user has no active membership
    /// or the role does not grant the permission. A team-scoped grant without a team
    /// falls back to own scope.
    /// </summary>
    Task<DataScope> ReadAsync(Guid userId, Guid organizationId, string permission, CancellationToken cancellationToken = default);
}
