namespace Parley.Crm.Shared.Pipelines;

/// <summary>Pipeline columns read in one statement, before its stages are read.</summary>
public sealed record PipelineHeader(Guid Id, string Name, string? SystemKey, bool IsDefault, uint Version);
