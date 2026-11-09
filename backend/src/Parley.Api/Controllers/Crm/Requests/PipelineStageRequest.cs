namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a stage create or update: name, color index and win probability in basis points.</summary>
public sealed record PipelineStageRequest(string? Name, int ColorIndex, int ProbabilityBps);
