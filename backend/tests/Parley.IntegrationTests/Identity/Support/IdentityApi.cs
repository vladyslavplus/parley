using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.SharedKernel.Localization;
using Parley.SharedKernel.Tenancy.Abstractions;

namespace Parley.IntegrationTests.Identity.Support;

/// <summary>
/// Helpers for calling the identity endpoints in tests: authorized clients, sign-in, route
/// templates filled with identifiers, and reading the organization claim of an access token.
/// </summary>
public static class IdentityApi
{
    private const string IdPlaceholder = "{id:guid}";

    public static HttpClient CreateAuthorizedClient(WebApplicationFactory<Program> factory, string language, string accessToken)
    {
        var client = AuthTestClient.CreateClient(factory, language);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Signs in with the shared test password and returns the issued pair.</summary>
    public static async Task<AuthTokensView> SignInAsync(WebApplicationFactory<Program> factory, string email, CancellationToken cancellationToken)
    {
        using var client = AuthTestClient.CreateClient(factory, CultureCodes.English);
        var response = await AuthTestClient.SignInAsync(client, email, AuthTestClient.Password, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await AuthTestClient.ReadTokensAsync(response, cancellationToken);
    }

    /// <summary>Fills an id placeholder of a route template, for example <c>members/{id:guid}/role</c>.</summary>
    public static string Route(string template, Guid id) =>
        template.Replace(IdPlaceholder, id.ToString("D"), StringComparison.Ordinal);

    /// <summary>Reads the organization claim of a signed access token. The signature is not checked here.</summary>
    public static string ReadOrganizationClaim(string accessToken)
    {
        var payload = accessToken.Split('.')[1];
        using var document = JsonDocument.Parse(Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(payload)));
        return document.RootElement.GetProperty(TenancyClaimTypes.OrganizationId).GetString()!;
    }
}
