namespace Parley.Api.Controllers.Crm.Requests;

/// <summary>Body of a note update: the row version the client read and the new text.</summary>
public sealed record UpdateNoteRequest(uint Version, string? Body);
