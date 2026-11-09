namespace Parley.Crm.Shared.Pipelines;

/// <summary>One stage of the default pipeline: the resource key of its name and its settings.</summary>
/// <param name="NameKey">Resource key of the stage name, resolved in the organization's locale.</param>
/// <param name="Kind">Stage kind, one of the pipeline stage kinds.</param>
/// <param name="ProbabilityBps">Win probability of the stage in basis points.</param>
/// <param name="ColorIndex">Index into the color ramp.</param>
public sealed record DefaultPipelineStage(string NameKey, string Kind, int ProbabilityBps, int ColorIndex);
