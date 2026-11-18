namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a stage create or update: name, colour and win probability in basis points.</summary>
public sealed record PipelineStageRequest(string? Name, string? Color, int ProbabilityBps);
