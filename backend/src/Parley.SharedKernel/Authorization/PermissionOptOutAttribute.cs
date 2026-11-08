namespace Parley.SharedKernel.Authorization;

/// <summary>
/// Explicit opt-out from the permission check, with the kind of access required.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PermissionOptOutAttribute(PermissionOptOutKind kind) : Attribute
{
    public PermissionOptOutKind Kind { get; } = kind;
}
