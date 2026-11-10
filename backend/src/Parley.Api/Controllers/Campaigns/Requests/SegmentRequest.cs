namespace Parley.Api.Controllers.Campaigns.Requests;

/// <summary>Body of a segment create or replace request.</summary>
/// <param name="Name">Segment name.</param>
/// <param name="Definition">Filter definition; null means an open filter that matches every eligible contact.</param>
/// <param name="Version">Row version the client read; required on replace, ignored on create.</param>
public sealed record SegmentRequest(string? Name, SegmentDefinitionRequest? Definition, uint? Version);
