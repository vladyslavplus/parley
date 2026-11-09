namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a bulk owner assignment for contacts or companies.</summary>
public sealed record BulkOwnerRequest(IReadOnlyList<Guid>? Ids, Guid? OwnerUserId);
