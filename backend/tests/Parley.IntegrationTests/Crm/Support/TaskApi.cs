using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Infrastructure.Auth;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Helpers for the task endpoints: create, state changes, read and list identifiers.</summary>
public static class TaskApi
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string VersionProperty = "version";
    private const string AssigneeProperty = "assigneeUserId";

    /// <summary>Creates a task; fails the test unless the response is 201.</summary>
    public static async Task<JsonElement> CreateAsync(
        HttpClient client,
        string title,
        DateTimeOffset? dueAt,
        Guid? assigneeUserId,
        Guid? contactId,
        CancellationToken cancellationToken)
    {
        var response = await client.PostAsJsonAsync(
            CrmRoutes.Tasks,
            new { title, dueAt, assigneeUserId, contactId },
            cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    public static Task<HttpResponseMessage> CompleteAsync(HttpClient client, Guid taskId, uint version, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(PipelineApi.Route(CrmRoutes.TaskComplete, taskId), new { version }, cancellationToken);

    public static Task<HttpResponseMessage> ReopenAsync(HttpClient client, Guid taskId, uint version, CancellationToken cancellationToken) =>
        client.PostAsJsonAsync(PipelineApi.Route(CrmRoutes.TaskReopen, taskId), new { version }, cancellationToken);

    public static async Task<JsonElement> ReadAsync(HttpClient client, Guid taskId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(PipelineApi.Route(CrmRoutes.TaskById, taskId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return await AuthTestClient.ReadJsonAsync(response, cancellationToken);
    }

    /// <summary>Identifiers of one page of a task list route, in list order.</summary>
    public static async Task<List<Guid>> ListIdsAsync(HttpClient client, string route, string query, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{route}?{query}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty).EnumerateArray().Select(item => item.GetProperty(IdProperty).GetGuid()).ToList();
    }

    public static Guid IdOf(JsonElement task) => task.GetProperty(IdProperty).GetGuid();

    public static uint VersionOf(JsonElement task) => task.GetProperty(VersionProperty).GetUInt32();

    public static Guid AssigneeOf(JsonElement task) => task.GetProperty(AssigneeProperty).GetGuid();
}
