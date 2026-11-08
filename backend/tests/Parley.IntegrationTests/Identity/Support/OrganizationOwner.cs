namespace Parley.IntegrationTests.Identity.Support;

/// <summary>An account that owns an organization (administrator) with a signed access token.</summary>
public sealed record OrganizationOwner(string Email, Guid UserId, Guid OrganizationId, string AccessToken);
