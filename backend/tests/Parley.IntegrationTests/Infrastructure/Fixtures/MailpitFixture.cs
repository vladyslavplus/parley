using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Parley.IntegrationTests.Notifications.Support;
using Xunit;

[assembly: AssemblyFixture(typeof(Parley.IntegrationTests.Infrastructure.Fixtures.MailpitFixture))]

namespace Parley.IntegrationTests.Infrastructure.Fixtures;


/// <summary>
/// One Mailpit container per test run. Tests read captured messages back
/// through the Mailpit HTTP API.
/// </summary>
public sealed class MailpitFixture : IAsyncLifetime
{
    /// <summary>Same tag and digest as the Mailpit service in docker-compose.yml.</summary>
    private const string Image = "axllent/mailpit:v1.27.8@sha256:6abc8e633df15eaf785cfcf38bae48e66f64beecdc03121e249d0f9ec15f0707";
    private const ushort SmtpContainerPort = 1025;
    private const ushort HttpContainerPort = 8025;
    private const string InfoPath = "/api/v1/info";
    private const string MessagesPath = "/api/v1/messages?limit=0";
    private const string MessageDetailPathFormat = "/api/v1/message/{0}";
    private const string MessagesListProperty = "messages";
    private const string MessageIdProperty = "ID";
    private const string ToProperty = "To";
    private const string AddressProperty = "Address";
    private const string SubjectProperty = "Subject";
    private const string TextProperty = "Text";
    private const string HtmlProperty = "HTML";

    private readonly IContainer _container = new ContainerBuilder(Image)
        .WithPortBinding(SmtpContainerPort, true)
        .WithPortBinding(HttpContainerPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(request => request.ForPort(HttpContainerPort).ForPath(InfoPath)))
        .Build();

    private HttpClient? _http;

    public string Host => _container.Hostname;

    public int SmtpPort => _container.GetMappedPublicPort(SmtpContainerPort);

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        _http = new HttpClient
        {
            BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(HttpContainerPort)}"),
        };
    }

    public async ValueTask DisposeAsync()
    {
        _http?.Dispose();
        await _container.DisposeAsync();
    }

    /// <summary>
    /// Finds the captured message addressed to <paramref name="recipient"/>.
    /// Mailpit stores a message before it acknowledges SMTP DATA, so the
    /// message is present as soon as the send call has returned.
    /// </summary>
    public async Task<CapturedEmail?> FindMessageAsync(string recipient, CancellationToken cancellationToken, string? subject = null)
    {
        var http = _http ?? throw new InvalidOperationException("Mailpit fixture is not initialized.");
        using var listDocument = await http.GetFromJsonAsync<JsonDocument>(MessagesPath, cancellationToken);
        if (listDocument is null || !listDocument.RootElement.TryGetProperty(MessagesListProperty, out var messages))
        {
            return null;
        }

        foreach (var summary in messages.EnumerateArray())
        {
            var id = summary.GetProperty(MessageIdProperty).GetString();
            using var detailDocument = await http.GetFromJsonAsync<JsonDocument>(
                string.Format(System.Globalization.CultureInfo.InvariantCulture, MessageDetailPathFormat, id),
                cancellationToken);
            var detail = detailDocument!.RootElement;

            var toAddresses = detail.GetProperty(ToProperty).EnumerateArray()
                .Select(address => address.GetProperty(AddressProperty).GetString() ?? string.Empty)
                .ToArray();
            var detailSubject = detail.GetProperty(SubjectProperty).GetString() ?? string.Empty;
            if (toAddresses.Contains(recipient, StringComparer.OrdinalIgnoreCase)
                && (subject is null || string.Equals(detailSubject, subject, StringComparison.Ordinal)))
            {
                return new CapturedEmail(
                    detail.GetProperty(SubjectProperty).GetString() ?? string.Empty,
                    detail.GetProperty(TextProperty).GetString() ?? string.Empty,
                    detail.GetProperty(HtmlProperty).GetString() ?? string.Empty,
                    toAddresses);
            }
        }

        return null;
    }
}
