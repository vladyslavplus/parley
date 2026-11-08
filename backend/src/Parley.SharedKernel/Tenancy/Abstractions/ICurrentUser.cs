using Parley.SharedKernel.Tenancy.Implementations;

namespace Parley.SharedKernel.Tenancy.Abstractions;

/// <summary>
/// Ambient user for the current scope. Background work provides it explicitly
/// through <see cref="JobCurrentUser"/>.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    Guid RequireUserId();
}
