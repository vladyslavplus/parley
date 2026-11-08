using Parley.SharedKernel.Authorization;

namespace Parley.Identity.Authorization.Behavior;

/// <summary>
/// Reads the authorization attributes of a request type once. Declaring both a
/// permission and an opt-out, or declaring neither, is a programming error and throws
/// when first used.
/// </summary>
public static class RequestAuthorizationDeclaration<TMessage>
{
    public static readonly AuthorizationDeclaration Value = Read(typeof(TMessage));

    private static AuthorizationDeclaration Read(Type messageType)
    {
        var permission = messageType.GetCustomAttributes(typeof(RequiresPermissionAttribute), inherit: false)
            .Cast<RequiresPermissionAttribute>()
            .SingleOrDefault();
        var optOut = messageType.GetCustomAttributes(typeof(PermissionOptOutAttribute), inherit: false)
            .Cast<PermissionOptOutAttribute>()
            .SingleOrDefault();

        if (permission is not null && optOut is not null)
        {
            throw new InvalidOperationException(
                $"Request '{messageType.FullName}' declares both a permission and an opt-out. Declare exactly one.");
        }

        if (permission is not null)
        {
            return new AuthorizationDeclaration(AuthorizationKind.Permission, permission.Permission);
        }

        return optOut?.Kind switch
        {
            PermissionOptOutKind.Anonymous => new AuthorizationDeclaration(AuthorizationKind.Anonymous, null),
            PermissionOptOutKind.AuthenticatedOnly => new AuthorizationDeclaration(AuthorizationKind.AuthenticatedOnly, null),
            _ => new AuthorizationDeclaration(AuthorizationKind.Undeclared, null),
        };
    }
}
