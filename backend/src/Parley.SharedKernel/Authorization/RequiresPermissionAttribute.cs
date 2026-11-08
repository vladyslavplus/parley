namespace Parley.SharedKernel.Authorization;

/// <summary>
/// Declares the permission a Mediator request requires. The authorization
/// pipeline behavior enforces it server-side through the identity module.
/// The permission value comes from the <c>Permissions</c> constants in
/// <c>Identity.Contracts</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class RequiresPermissionAttribute(string permission) : Attribute
{
    /// <summary>The permission string, for example <c>contacts.write</c>.</summary>
    public string Permission { get; } = permission;
}
