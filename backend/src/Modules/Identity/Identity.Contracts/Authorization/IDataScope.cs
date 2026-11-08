namespace Parley.Identity.Contracts.Authorization;

/// <summary>
/// Record visibility of the current request's user. Handlers filter queries by the
/// owner ids; a record outside the scope is reported as not found. The result is
/// resolved once per request and reused for each permission.
/// </summary>
public interface IDataScope
{
    /// <summary>The visibility of the current user for <paramref name="permission"/>.</summary>
    ValueTask<DataScope> ForPermissionAsync(string permission, CancellationToken cancellationToken = default);
}
