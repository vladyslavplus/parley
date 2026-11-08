namespace Parley.Identity.Authorization.Behavior;

/// <summary>The declared rule of one request type.</summary>
/// <param name="Kind">Which rule applies.</param>
/// <param name="Permission">The permission, when <see cref="Kind"/> is <see cref="AuthorizationKind.Permission"/>.</param>
public sealed record AuthorizationDeclaration(AuthorizationKind Kind, string? Permission);
