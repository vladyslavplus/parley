using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Parley.Api.Controllers.Auth;
using Parley.IntegrationTests.Infrastructure.Fixtures;
using Parley.IntegrationTests.Notifications.Support;

namespace Parley.IntegrationTests.Infrastructure.Auth;

/// <summary>
/// Calls the authentication endpoints the way a browser-facing client does, and
/// reads account emails back from Mailpit.
/// </summary>
public static class AuthTestClient
{
    public const string Password = "Correct-password-1";

    /// <summary>Subject of the English password reset email.</summary>
    public const string EnResetSubject = "Reset your password";

    private const string EmailDomain = "parley.test";

    private const string UserIdParameter = "userId";

    private const string TokenParameter = "token";

    private const string AccessTokenProperty = "accessToken";

    private const string RefreshTokenProperty = "refreshToken";

    private static readonly TimeSpan EmailWaitTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan EmailPollInterval = TimeSpan.FromMilliseconds(200);

    private static readonly Regex LinkPattern = new(
        $@"{UserIdParameter}=(?<user>[0-9a-fA-F-]{{36}})&{TokenParameter}=(?<token>\S+)",
        RegexOptions.CultureInvariant);

    public static HttpClient CreateClient(WebApplicationFactory<Program> factory, string language)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
        return client;
    }

    public static string UniqueEmail() => $"account-{Guid.CreateVersion7():N}@{EmailDomain}";

    public static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(
            Route(AuthRoutes.Register),
            new { email, password, organizationName = "Test organization" },
            cancellationToken);

    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid userId, string token, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(Route(AuthRoutes.ConfirmEmail), new { userId, token }, cancellationToken);

    public static Task<HttpResponseMessage> SignInAsync(HttpClient client, string email, string password, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(Route(AuthRoutes.SignIn), new { email, password }, cancellationToken);

    public static Task<HttpResponseMessage> RefreshAsync(HttpClient client, string refreshToken, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(Route(AuthRoutes.Refresh), new { refreshToken }, cancellationToken);

    public static Task<HttpResponseMessage> SignOutAsync(HttpClient client, string refreshToken, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(Route(AuthRoutes.SignOut), new { refreshToken }, cancellationToken);

    public static Task<HttpResponseMessage> ForgotPasswordAsync(HttpClient client, string email, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(Route(AuthRoutes.ForgotPassword), new { email }, cancellationToken);

    public static Task<HttpResponseMessage> ResetPasswordAsync(HttpClient client, Guid userId, string token, string newPassword, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(Route(AuthRoutes.ResetPassword), new { userId, token, newPassword }, cancellationToken);

    /// <summary>Polls Mailpit until a message for <paramref name="recipient"/> arrives.</summary>
    public static async Task<CapturedEmail> WaitForEmailAsync(
        MailpitFixture mailpit,
        string recipient,
        CancellationToken cancellationToken,
        string? subject = null)
    {
        var deadline = DateTimeOffset.UtcNow + EmailWaitTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            var message = await mailpit.FindMessageAsync(recipient, cancellationToken, subject);
            if (message is not null)
            {
                return message;
            }

            await Task.Delay(EmailPollInterval, cancellationToken);
        }

        throw new TimeoutException($"No email arrived for {recipient} within {EmailWaitTimeout}.");
    }

    /// <summary>Reads the user id and the token out of the confirmation or reset link in the mail.</summary>
    public static (Guid UserId, string Token) ReadLink(CapturedEmail message)
    {
        var match = LinkPattern.Match(message.Text);
        if (!match.Success)
        {
            throw new InvalidOperationException("The email carries no account link.");
        }

        return (Guid.Parse(match.Groups["user"].Value), Uri.UnescapeDataString(match.Groups["token"].Value));
    }

    /// <summary>Registers, confirms through the emailed link, and returns the account identity.</summary>
    public static async Task<ConfirmedAccount> CreateConfirmedAccountAsync(
        HttpClient client,
        MailpitFixture mailpit,
        CancellationToken cancellationToken)
    {
        var email = UniqueEmail();
        var registered = await RegisterAsync(client, email, Password, cancellationToken);
        registered.EnsureSuccessStatusCode();

        var message = await WaitForEmailAsync(mailpit, email, cancellationToken);
        var (userId, token) = ReadLink(message);
        var confirmed = await ConfirmAsync(client, userId, token, cancellationToken);
        confirmed.EnsureSuccessStatusCode();
        return new ConfirmedAccount(email, userId);
    }

    public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.Clone();
    }

    public static async Task<AuthTokensView> ReadTokensAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await ReadJsonAsync(response, cancellationToken);
        return new AuthTokensView(
            body.GetProperty(AccessTokenProperty).GetString()!,
            body.GetProperty(RefreshTokenProperty).GetString()!);
    }

    public static string Route(string segment) => $"{AuthRoutes.Base}/{segment}";
}
