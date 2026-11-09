namespace Parley.Crm.Shared.Pipelines;

/// <summary>Sort fields accepted by the pipeline list.</summary>
public static class PipelineSortFields
{
    /// <summary>Pipeline name. Default.</summary>
    public const string Name = "name";

    public static IReadOnlyList<string> Values { get; } = [Name];

    public static bool IsValid(string? value) => value is null || Values.Contains(value);
}
