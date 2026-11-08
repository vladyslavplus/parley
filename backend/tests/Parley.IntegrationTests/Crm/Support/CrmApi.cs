using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.Api.Controllers.Identity;
using Parley.IntegrationTests.Identity.Members.Support;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;
using Parley.IntegrationTests.Infrastructure.Hosting;
using Parley.SharedKernel.Localization;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Helpers for the CRM endpoints: authorized clients, member setup, and response readers.</summary>
public static class CrmApi
{
    private const string IdProperty = "id";
    private const string VersionProperty = "version";

    /// <summary>An organization member with a signed access token.</summary>
    public sealed record Member(Guid UserId, string AccessToken);

    public static HttpClient Client(IdentityApiFactory factory, string accessToken) =>
        IdentityApi.CreateAuthorizedClient(factory, CultureCodes.English, accessToken);

    /// <summary>Creates a confirmed member with the role and signs it in.</summary>
    public static async Task<Member> AddMemberAsync(IdentityApiFactory factory, Guid organizationId, Guid roleId, CancellationToken cancellationToken)
    {
        var (userId, email) = await IdentitySetup.AddMemberAsync(factory, organizationId, roleId, cancellationToken);
        var tokens = await IdentityApi.SignInAsync(factory, email, cancellationToken);
        return new Member(userId, tokens.AccessToken);
    }

    /// <summary>Creates a team as the administrator and places each member in it.</summary>
    public static async Task CreateTeamAsync(
        IdentityApiFactory factory,
        HttpClient admin,
        Guid organizationId,
        IEnumerable<Guid> memberUserIds,
        CancellationToken cancellationToken)
    {
        var teamResponse = await admin.PostAsJsonAsync(IdentityRoutes.Teams, new { name = $"Team {Guid.CreateVersion7():N}" }, cancellationToken);
        teamResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamId = (await AuthTestClient.ReadJsonAsync(teamResponse, cancellationToken)).GetProperty(IdProperty).GetGuid();

        foreach (var userId in memberUserIds)
        {
            var membershipId = await MembershipTestData.ReadMembershipIdAsync(factory.Services, organizationId, userId, cancellationToken);
            var placed = await admin.PutAsJsonAsync(
                IdentityApi.Route(IdentityRoutes.MemberTeam, membershipId),
                new { teamId },
                cancellationToken);
            placed.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }

    /// <summary>Creates a contact and returns the view; fails the test when the call is not 200.</summary>
    public static async Task<JsonElement> CreateContactAsync(HttpClient client, ContactBody body, CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(CrmRoutes.Contacts, body, cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    public static Guid IdOf(JsonElement view) => view.GetProperty(IdProperty).GetGuid();

    public static uint VersionOf(JsonElement view) => view.GetProperty(VersionProperty).GetUInt32();

    public static string ContactRoute(Guid id) => IdentityApi.Route(CrmRoutes.ContactById, id);

    public static string CompanyRoute(Guid id) => IdentityApi.Route(CrmRoutes.CompanyById, id);

    /// <summary>Reads the code of the first error on one field of a ProblemDetails response; the field name matches ignoring case.</summary>
    public static async Task<string?> FieldErrorCodeAsync(HttpResponseMessage response, string field, CancellationToken cancellationToken)
    {
        var problem = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        var errors = problem.GetProperty(Parley.Api.ApiConstants.ProblemErrors);
        var key = errors.EnumerateObject()
            .Select(property => property.Name)
            .Single(name => string.Equals(name, field, StringComparison.OrdinalIgnoreCase));
        return errors.GetProperty(key)[0].GetProperty("code").GetString();
    }

    /// <summary>Reads the error code of a ProblemDetails response.</summary>
    public static async Task<string?> ErrorCodeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var problem = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return problem.TryGetProperty(Parley.Api.ApiConstants.ProblemCode, out var code) ? code.GetString() : null;
    }
}
