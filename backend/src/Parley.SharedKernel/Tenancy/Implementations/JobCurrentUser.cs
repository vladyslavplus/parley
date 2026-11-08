using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.SharedKernel.Tenancy.Implementations;

/// <summary>
/// User for background work. Settable per execution flow.
/// </summary>
public sealed class JobCurrentUser : ICurrentUser
{
    private static readonly AsyncLocal<Guid?> AmbientUserId = new();

    public Guid? UserId => AmbientUserId.Value;

    public Guid RequireUserId() => UserId ?? throw new UserContextNotSetException();

    public void Set(Guid userId) => AmbientUserId.Value = userId;

    public void Clear() => AmbientUserId.Value = null;
}
