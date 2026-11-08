using Mediator;
using Parley.Identity.Contracts.Authorization;
using Parley.Identity.Contracts.Errors;
using Parley.SharedKernel.Results;
using Parley.SharedKernel.Tenancy.Abstractions;
using Parley.SharedKernel.Tenancy.Exceptions;

namespace Parley.Identity.Authorization.Behavior;

/// <summary>
/// Enforces the declared access rule of every Mediator request, server-side. The
/// organization comes from the access-token claim and must have an active membership.
/// A missing permission returns the shared forbidden error; a missing session returns
/// unauthorized. A request that declares nothing throws instead of running.
/// </summary>
public sealed class PermissionAuthorizationBehavior<TMessage, TResponse>(
    IPermissionChecker permissionChecker,
    ICurrentUser currentUser,
    ITenantContext tenantContext) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
    where TResponse : IResult<TResponse>
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        var declaration = RequestAuthorizationDeclaration<TMessage>.Value;
        if (declaration.Kind == AuthorizationKind.Undeclared)
        {
            throw new InvalidOperationException(
                $"Request '{typeof(TMessage).FullName}' declares no permission and no opt-out. " +
                "Add RequiresPermission or PermissionOptOut.");
        }

        if (declaration.Kind == AuthorizationKind.Anonymous)
        {
            return await next(message, cancellationToken);
        }

        var userId = currentUser.UserId;
        if (userId is null || !TryGetOrganizationId(out var organizationId))
        {
            return TResponse.Failure(Error.Unauthorized(IdentityErrorCodes.AuthTokenInvalid));
        }

        var allowed = declaration.Kind == AuthorizationKind.Permission
            ? await permissionChecker.HasPermissionAsync(userId.Value, organizationId, declaration.Permission!, cancellationToken)
            : await permissionChecker.IsActiveMemberAsync(userId.Value, organizationId, cancellationToken);
        if (!allowed)
        {
            return TResponse.Failure(Error.Forbidden(IdentityErrorCodes.PermissionDenied));
        }

        return await next(message, cancellationToken);
    }

    private bool TryGetOrganizationId(out Guid organizationId)
    {
        try
        {
            organizationId = tenantContext.OrganizationId;
            return organizationId != Guid.Empty;
        }
        catch (TenantContextNotSetException)
        {
            organizationId = Guid.Empty;
            return false;
        }
    }
}
