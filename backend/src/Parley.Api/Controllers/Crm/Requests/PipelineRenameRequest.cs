namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a pipeline rename. <c>version</c> is the row version the client read.</summary>
public sealed record PipelineRenameRequest(uint Version, string? Name);
