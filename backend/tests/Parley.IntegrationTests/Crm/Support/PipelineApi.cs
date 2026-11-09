using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Parley.Api.Controllers.Crm;
using Parley.IntegrationTests.Infrastructure.Auth;

namespace Parley.IntegrationTests.Crm.Support;

/// <summary>Helpers for the pipeline endpoints: route building, the default pipeline and its stages.</summary>
public static class PipelineApi
{
    private const string ItemsProperty = "items";
    private const string IdProperty = "id";
    private const string NameProperty = "name";
    private const string KindProperty = "kind";
    private const string StagesProperty = "stages";
    private const string PositionProperty = "position";
    private const string FirstPageLimit = "limit=1";

    private static readonly Regex GuidPlaceholder = new(@"\{\w+:guid\}");

    /// <summary>A stage as the tests read it.</summary>
    public sealed record Stage(Guid Id, string Name, string Kind, int Position);

    /// <summary>Fills the guid placeholders of a route template, in order.</summary>
    public static string Route(string template, params Guid[] ids)
    {
        var index = 0;
        return GuidPlaceholder.Replace(template, _ => ids[index++].ToString("D"));
    }

    /// <summary>The identifier of the organization's default pipeline, read through the list endpoint.</summary>
    public static async Task<Guid> DefaultPipelineIdAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync($"{CrmRoutes.Pipelines}?{FirstPageLimit}", cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await AuthTestClient.ReadJsonAsync(response, cancellationToken);
        return page.GetProperty(ItemsProperty)[0].GetProperty(IdProperty).GetGuid();
    }

    /// <summary>The stages of a pipeline in position order.</summary>
    public static async Task<IReadOnlyList<Stage>> StagesAsync(HttpClient client, Guid pipelineId, CancellationToken cancellationToken)
    {
        var response = await client.GetAsync(Route(CrmRoutes.PipelineById, pipelineId), cancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return ParseStages(await AuthTestClient.ReadJsonAsync(response, cancellationToken));
    }

    /// <summary>Reads the stages of a pipeline view, as the API returns it.</summary>
    public static IReadOnlyList<Stage> ParseStages(JsonElement pipeline) =>
        pipeline.GetProperty(StagesProperty).EnumerateArray()
            .Select(stage => new Stage(
                stage.GetProperty(IdProperty).GetGuid(),
                stage.GetProperty(NameProperty).GetString()!,
                stage.GetProperty(KindProperty).GetString()!,
                stage.GetProperty(PositionProperty).GetInt32()))
            .ToList();

    /// <summary>The stage with the given name.</summary>
    public static Stage Named(IReadOnlyList<Stage> stages, string name) => stages.Single(stage => stage.Name == name);
}
