using Microsoft.Extensions.Options;
using Parley.Identity.Options.Auth;

namespace Parley.Identity.Email.Links;

/// <summary>
/// Builds the links in account emails. The token travels in the query string of a
/// page on the web app, which posts it back to the API. Tokens are URL-encoded.
/// </summary>
public sealed class AuthEmailLinks(IOptions<AuthOptions> options)
{
    private const string UserIdParameter = "userId";
    private const string TokenParameter = "token";

    private readonly AuthOptions _options = options.Value;

    public string ConfirmEmail(Guid userId, string token) =>
        BuildLink(_options.ConfirmEmailPath, userId, token);

    public string ResetPassword(Guid userId, string token) =>
        BuildLink(_options.ResetPasswordPath, userId, token);

    /// <summary>Link to the invitation page. Carries the token only; the page identifies the invitation by its hash.</summary>
    public string AcceptInvitation(string token) =>
        $"{_options.PublicBaseUrl.TrimEnd('/')}{_options.AcceptInvitationPath}?{TokenParameter}={Uri.EscapeDataString(token)}";

    private string BuildLink(string path, Guid userId, string token) =>
        $"{_options.PublicBaseUrl.TrimEnd('/')}{path}?{UserIdParameter}={userId:D}&{TokenParameter}={Uri.EscapeDataString(token)}";
}
