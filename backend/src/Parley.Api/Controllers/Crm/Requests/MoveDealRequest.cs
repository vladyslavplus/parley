namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a deal move: target stage, zero-based place in that stage, and the row version the client read.</summary>
public sealed record MoveDealRequest(Guid? StageId, int Position, uint Version, string? LostReason);
