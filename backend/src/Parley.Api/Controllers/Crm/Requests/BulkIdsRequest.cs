namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a bulk delete for contacts or companies.</summary>
public sealed record BulkIdsRequest(IReadOnlyList<Guid>? Ids);
