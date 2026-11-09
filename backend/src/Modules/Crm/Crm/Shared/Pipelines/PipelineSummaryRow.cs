namespace Parley.Crm.Shared.Pipelines;

/// <summary>A pipeline list row. The name is the stored one, which the keyset cursor orders by; it is shown through <see cref="PipelineNames"/>.</summary>
public sealed record PipelineSummaryRow(Guid Id, string Name, string? SystemKey, bool IsDefault, int StageCount, uint Version);
