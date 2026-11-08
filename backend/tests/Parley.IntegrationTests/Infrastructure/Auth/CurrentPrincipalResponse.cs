namespace Parley.IntegrationTests.Infrastructure.Auth;

public sealed record CurrentPrincipalResponse(string? Subject, string? OrganizationId);
