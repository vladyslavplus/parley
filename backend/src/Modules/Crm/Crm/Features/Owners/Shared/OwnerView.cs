namespace Parley.Crm.Features.Owners.Shared;

/// <summary>A person who can own a record, as an owner picker shows them.</summary>
/// <param name="Id">User identifier, sent back as <c>ownerUserId</c>.</param>
/// <param name="DisplayName">Display name, or null when the user has none.</param>
/// <param name="Email">Email address, or null when unknown.</param>
/// <param name="IsActive">True while the membership in the organization is active.</param>
public sealed record OwnerView(Guid Id, string? DisplayName, string? Email, bool IsActive);
