using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Identity.Support;
using Parley.IntegrationTests.Infrastructure.Auth;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Helpers for the contact export endpoints: request with an idempotency key, poll the status, download.</summary>
public static class ContactExportApi
{
    private const string IdempotencyKeyHeader = "Idempotency-Key";
    private const string StatusProperty = "status";
    private const string CompletedStatus = "completed";
    private const int MaximumPollAttempts = 200;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    public static string ExportRoute(Guid id) => IdentityApi.Route(CrmRoutes.ContactExportById, id);

    public static string DownloadRoute(Guid id) => IdentityApi.Route(CrmRoutes.ContactExportDownload, id);

    /// <summary>Posts an export request with the given idempotency key and returns the response.</summary>
    public static async Task<HttpResponseMessage> RequestAsync(
        HttpClient client,
        string? idempotencyKey,
        object body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, CrmRoutes.ContactExports);
        request.Content = JsonContent.Create(body);
        if (idempotencyKey is not null)
        {
            request.Headers.Add(IdempotencyKeyHeader, idempotencyKey);
        }

        return await client.SendAsync(request, cancellationToken);
    }

    /// <summary>Reads the export id from an accepted response.</summary>
    public static async Task<Guid> ExportIdOfAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var view = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return view.GetProperty("id").GetGuid();
    }

    /// <summary>Polls the status until the background job completes the file.</summary>
    public static async Task WaitForCompletedAsync(HttpClient client, Guid exportId, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaximumPollAttempts; attempt++)
        {
            var response = await client.GetAsync(ExportRoute(exportId), cancellationToken);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var view = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
            if (view.GetProperty(StatusProperty).GetString() == CompletedStatus)
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new TimeoutException($"Contact export {exportId} did not complete in time.");
    }

    public static async Task<JsonElement> StatusAsync(HttpClient client, Guid exportId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(ExportRoute(exportId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }
}
