namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// The resolved visibility for one user and one permission. For
/// <see cref="DataScopeLevel.Organization"/> the owner ids are null (no owner filter).
/// For other levels they list the visible owner ids; unowned records are never in
/// that list, so they are visible at organization level only.
/// </summary>
/// <param name="Level">Visibility level taken from the role grant.</param>
/// <param name="OwnerUserIds">Visible owner ids, or null when the level has no owner filter.</param>
public sealed record DataScope(DataScopeLevel Level, IReadOnlyCollection<Guid>? OwnerUserIds)
{
    /// <summary>Scope with no visible records.</summary>
    public static DataScope Nothing { get; } = new(DataScopeLevel.None, []);
}
