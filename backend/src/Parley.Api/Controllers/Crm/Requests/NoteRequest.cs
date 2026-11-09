namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a note create. Exactly one of the three subject links must be set.</summary>
public sealed record NoteRequest(string? Body, Guid? ContactId, Guid? DealId, Guid? CompanyId);
