namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a stage reorder: every open stage identifier in the new order.</summary>
public sealed record PipelineStageOrderRequest(IReadOnlyList<Guid>? StageIds);
