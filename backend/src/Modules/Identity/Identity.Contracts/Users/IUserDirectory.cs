namespace Parley.Identity.Contracts.Users;

/// <summary>
/// Names of the people of one organization, for other modules that show a user (for example a record
/// owner). Only users with a membership in the organization are returned, so an identifier from
/// another organization never resolves. One call serves a whole batch.
/// </summary>
public interface IUserDirectory
{
    /// <summary>Returns the entries of the given users that are members of the organization, keyed by user id.</summary>
    /// <param name="organizationId">The organization whose memberships limit the result.</param>
    /// <param name="userIds">The user ids to resolve.</param>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    Task<IReadOnlyDictionary<Guid, UserDirectoryEntry>> FindMembersAsync(
        Guid organizationId,
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches the active members of the organization by display name or email, ordered by name. Only
    /// members with an active membership are returned; a deactivated member still resolves by id through
    /// <see cref="FindMembersAsync"/>.
    /// </summary>
    /// <param name="organizationId">The organization whose members are searched.</param>
    /// <param name="term">Text the name or email must contain, or null for no text filter.</param>
    /// <param name="userIds">Users the result is limited to, or null for every active member.</param>
    /// <param name="limit">Most entries returned.</param>
    /// <param name="cancellationToken">Cancels the search.</param>
    Task<IReadOnlyList<UserDirectoryEntry>> SearchActiveMembersAsync(
        Guid organizationId,
        string? term,
        IReadOnlyCollection<Guid>? userIds,
        int limit,
        CancellationToken cancellationToken = default);
}
